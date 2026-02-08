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
            .Combine(context.CompilationProvider.Select((cp, _) => new WeakReference<Compilation>(cp)))
            .WithComparer(new ControlComparer());

        IncrementalValuesProvider<ControlResult> results = controlContexts
            .Select((i, _) => Generate(i))
            .Where(i => i is not null)!;

        context.RegisterSourceOutput(results, (spc, source) =>
        {
            foreach (var diagnostic in source.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (source.Context is { Code: { } code })
            {
                spc.AddSource(source.Context.ClassName, code);
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

                return builder.ToImmutable();
            });

        var allTypes = types
            .Combine(referencedControls)
            .Combine(currentNamespace);

        context.RegisterSourceOutput(allTypes, (spc, tuple) =>
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
        });
    }

    protected abstract string? GetGenerateAssemblyTypeProvider(ImmutableArray<ControlType> source, string? rootNamespace);

    private ControlResult? Generate((ControlContext Left, WeakReference<Compilation> Right) items)
    {
        var ((path, content, rootNamespace, projectDirectory, namespaces), weakReference) = items;

        if (!weakReference.TryGetTarget(out var compilation))
        {
            throw new InvalidOperationException("Compilation is null");
        }

        rootNamespace ??= compilation.AssemblyName;

        try
        {
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
    }

    protected abstract string GenerateCode(DesignerModel output);
}

public class ControlComparer : IEqualityComparer<(ControlContext Left, WeakReference<Compilation> Right)>
{
    public bool Equals((ControlContext Left, WeakReference<Compilation> Right) x, (ControlContext Left, WeakReference<Compilation> Right) y)
    {
        return x.Left.Equals(y.Left);
    }

    public int GetHashCode((ControlContext Left, WeakReference<Compilation> Right) obj)
    {
        return obj.Left.GetHashCode();
    }
}

public record ControlContext(string Path, string Content, string? RootNamespace, string? ProjectDirectory, ImmutableArray<KeyValuePair<string, string>> Namespaces);

