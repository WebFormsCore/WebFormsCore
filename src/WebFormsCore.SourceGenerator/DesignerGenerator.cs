using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WebFormsCore;
using WebFormsCore.Nodes;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

public abstract class DesignerGenerator : IIncrementalGenerator
{
    private const string LogDirectory = @"D:\Temp";

    private static void LogException(string context, Exception ex)
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            var logFile = Path.Combine(LogDirectory, $"WebFormsCore.SourceGenerator.{DateTime.Now:yyyyMMdd}.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logFile, message);
        }
        catch
        {
            // Ignore logging failures
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Directory of the project
        var mainSyntaxTree = context.CompilationProvider
            .Select((compilation, _) => compilation.SyntaxTrees.FirstOrDefault(x => x.HasCompilationUnitRoot)?.FilePath);

        var projectDirectory = context.AnalyzerConfigOptionsProvider
            .Select((options, _) => options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var directory) ? directory : null)
            .Combine(mainSyntaxTree)
            .Select((i, _) => i.Left ?? Path.GetDirectoryName(i.Right))
            .Select((d, _) => d?.Replace('\\', '/'));

        // Current namespace
        var currentNamespace = context.AnalyzerConfigOptionsProvider
            .Select((options, _) => options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns) ? ns : null);

        // Global namespaces
        var namespaces = context.AdditionalTextsProvider
            .Where(a => a.Path.EndsWith("web.config", StringComparison.OrdinalIgnoreCase))
            .SelectMany((a, _) => RootNode.GetNamespaces(a.GetText()?.ToString()))
            .Collect();

        // Controls
        var files = context.AdditionalTextsProvider
            .Where(a => a.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ||
                        a.Path.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase) ||
                        a.Path.EndsWith(".master", StringComparison.OrdinalIgnoreCase))
            .Select((text, cancellationToken) => (path: text.Path, content: text.GetText(cancellationToken)?.ToString() ?? ""));

        var controlContexts = files
            .Combine(projectDirectory.Combine(namespaces).Combine(currentNamespace))
            .Select((i, _) => new ControlContext(
                Path: i.Left.path,
                Content: i.Left.content,
                RootNamespace: i.Right.Right,
                ProjectDirectory: i.Right.Left.Left,
                Namespaces: i.Right.Left.Right))
            .Combine(context.CompilationProvider);

        IncrementalValuesProvider<ControlResult> results = controlContexts
            .Select((i, _) => Generate(i.Left, i.Right))
            .Where(i => i is not null)!;

        context.RegisterSourceOutput(results, (spc, source) =>
        {
            try
            {
                foreach (var diagnostic in source.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (source.Context is { Code: { } code })
                {
                    spc.AddSource(source.Context.ClassName, code);
                }
            }
            catch (Exception ex)
            {
                LogException("RegisterSourceOutput:results", ex);
                throw;
            }
        });

        var types = results
            .Select((i, _) => i.Context)
            .Where(i => i is not null)
            .Select((i, _) => new ControlType(
                RootNamespace: i!.RootNamespace,
                RelativePath: i.RelativePath,
                CompiledViewType: i.CompiledViewType
            ))
            .Collect();

        var referencedControls = context.CompilationProvider
            .Select((compilation, _) =>
            {
                var builder = ImmutableArray.CreateBuilder<ControlType>();

                foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
                {
                    foreach (var attribute in assembly.GetAttributes())
                    {
                        if (attribute is not
                            {
                                AttributeClass.Name: "AssemblyViewAttribute",
                                ConstructorArguments:
                                [
                                    { Value: string path },
                                    { Value: INamedTypeSymbol type }
                                ]
                            })
                        {
                            continue;
                        }

                        builder.Add(new ControlType(
                            RootNamespace: null,
                            RelativePath: path,
                            CompiledViewType: type.ToDisplayString()
                        ));
                    }
                }

                return new EquatableArray<ControlType>(builder.ToImmutable());
            });

        var allTypes = types
            .Combine(referencedControls)
            .Combine(currentNamespace);

        context.RegisterSourceOutput(allTypes, (spc, tuple) =>
        {
            try
            {
                var ((source, referencedControls), rootNamespace) = tuple;
                var addedPaths = new HashSet<string>();
                var builder = ImmutableArray.CreateBuilder<ControlType>();

                rootNamespace ??= source.FirstOrDefault(i => i.RootNamespace != null)?.RootNamespace;

                foreach (var type in source)
                {
                    if (addedPaths.Add(type.RelativePath))
                    {
                        builder.Add(type);
                    }
                }

                foreach (var type in referencedControls)
                {
                    if (addedPaths.Add(type.RelativePath))
                    {
                        builder.Add(type);
                    }
                }

                var code = GetGenerateAssemblyTypeProvider(builder.ToImmutable(), rootNamespace);

                if (code != null)
                {
                    spc.AddSource("GenerateAssemblyTypeProvider", code);
                }
            }
            catch (Exception ex)
            {
                LogException("RegisterSourceOutput:allTypes", ex);
                throw;
            }
        });
    }

    protected abstract string? GetGenerateAssemblyTypeProvider(ImmutableArray<ControlType> source, string? rootNamespace);

    private ControlResult? Generate(ControlContext controlContext, Compilation compilation)
    {
        try
        {
            var (path, content, rootNamespace, projectDirectory, namespaces) = controlContext;

            rootNamespace ??= compilation.AssemblyName;

            var fullPathRaw = projectDirectory is null ? path : Path.Combine(projectDirectory, path);
            var fullPath = fullPathRaw.Replace('\\', '/');
            var relativePath = fullPath;

            if (projectDirectory is not null && relativePath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(projectDirectory!.Length).TrimStart('\\', '/');
            }

            if (RootNode.Parse(
                out var diagnostics,
                compilation,
                fullPath,
                content,
                rootNamespace,
                namespaces,
                relativePath: relativePath,
                rootDirectory: projectDirectory) is not { } type)
            {
                return new ControlResult(diagnostics);
            }

            if (type.Inherits == null)
            {
                return new ControlResult(diagnostics);
            }

            var types = new List<RootNode> { type };
            var model = new DesignerModel(types, rootNamespace);
            var output = GenerateCode(model);

            if (type.ClassName is not { } className)
            {
                return new ControlResult(diagnostics);
            }

            return new ControlResult(
                diagnostics,
                new ControlResultContext(
                    RootNamespace: rootNamespace,
                    ClassName: className,
                    Content: output,
                    RelativePath: relativePath,
                    Namespace: type.Namespace,
                    CompiledViewType: type.DesignerFullTypeName,
                    Code: GenerateCode(model)
                )
            );
        }
        catch (Exception ex)
        {
            LogException($"Generate:{controlContext.Path}", ex);

            try
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.SourceGeneratorException,
                    Location.None,
                    ex.ToString());

                return new ControlResult(
                    EquatableArray.FromEnumerable(new[]
                    {
                        ReportedDiagnostic.Create(diagnostic.Descriptor, diagnostic.Location)
                    })
                );
            }
            catch (Exception innerEx)
            {
                LogException($"Generate:DiagnosticCreation:{controlContext.Path}", innerEx);
                // If even creating the diagnostic fails, return null to avoid crashing
                return null;
            }
        }
    }

    protected abstract string GenerateCode(DesignerModel output);
}

public record ControlContext(string Path, string Content, string? RootNamespace, string? ProjectDirectory, EquatableArray<KeyValuePair<string, string>> Namespaces);
