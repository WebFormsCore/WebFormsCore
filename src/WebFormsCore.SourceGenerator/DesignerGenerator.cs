using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WebFormsCore;
using WebFormsCore.Nodes;

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
                        a.Path.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
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

        context.RegisterSourceOutput(controlContexts, Generate);
    }

    private void Generate(SourceProductionContext context, (ControlContext Left, WeakReference<Compilation> Right) items)
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

            if (RootNode.Parse(compilation, fullPath, content, rootNamespace, namespaces, context: context, relativePath: relativePath, rootDirectory: projectDirectory) is not { } type) return;
            if (type.Inherits == null) return;

            var types = new List<RootNode> { type };
            var model = new DesignerModel(types, rootNamespace);
            var output = GenerateCode(context, model);

            if (type.ClassName is { } className)
            {
                context.AddSource(className, output);

                var counterPath = @$"C:\Temp\{className}.txt";

                if (File.Exists(counterPath))
                {
                    var counter = int.Parse(File.ReadAllText(counterPath));
                    File.WriteAllText(counterPath, (counter + 1).ToString());
                }
                else
                {
                    File.WriteAllText(counterPath, "1");
                }
            }
        }
        catch(Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "WF0001",
                    title: "WebFormsCore",
                    messageFormat: ex.ToString(),
                    category: "WebFormsCore",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None));
        }
    }

    protected abstract string GenerateCode(SourceProductionContext context, DesignerModel output);
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

