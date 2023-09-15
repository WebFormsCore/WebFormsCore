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
        var files = context.AdditionalTextsProvider
            .Where(a => a.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ||
                        a.Path.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase) ||
                        a.Path.EndsWith("web.config", StringComparison.OrdinalIgnoreCase))
            .Select((a, c) => (a.Path, a.GetText(c)!.ToString().ReplaceLineEndings("\n")));

        var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());
        var options = context.AnalyzerConfigOptionsProvider.Combine(compilationAndFiles);

        context.RegisterSourceOutput(options, Generate);
    }
    
    public void Generate(SourceProductionContext context, (AnalyzerConfigOptionsProvider Left, (Compilation Left, ImmutableArray<(string Path, string)> Right) Right) sourceContext)
    {
        var (analyzer, (compilation, files)) = sourceContext;
        var typesByClass = new Dictionary<string, RootNode>();
        var types = new List<RootNode>();
        var visited = new HashSet<string>();

        if (!analyzer.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var directory))
        {
            var mainSyntaxTree = compilation.SyntaxTrees.First(x => x.HasCompilationUnitRoot);
            directory = Path.GetDirectoryName(mainSyntaxTree.FilePath);
        }

        directory = directory?.Replace('\\', '/');

        if (!analyzer.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns))
        {
            ns = compilation.AssemblyName;
        }

        var (webConfigPath, webConfigText) = files.FirstOrDefault(x => x.Path.EndsWith("web.config", StringComparison.OrdinalIgnoreCase));
        var namespaces = RootNode.GetNamespaces(webConfigText);

        foreach (var (fullPathRaw, text) in files)
        {
            if (fullPathRaw == webConfigPath) continue;

            try
            {
                var fullPath = fullPathRaw.Replace('\\', '/');
                var relativePath = fullPath;

                if (directory != null && relativePath.StartsWith(directory))
                {
                    relativePath = relativePath.Substring(directory.Length + 1);
                }

                if (!visited.Add(fullPath)) continue;
                if (RootNode.Parse(compilation, fullPath, text, ns, namespaces, context: context, relativePath: relativePath, rootDirectory: directory) is not { } type) continue;
                if (type.Inherits == null) continue;

                var fullName = type.Inherits.ContainingNamespace.ToDisplayString() + "." + type.Inherits.Name;

                if (typesByClass.TryGetValue(fullName, out var existing))
                {
                    existing.Add(type);
                }
                else
                {
                    typesByClass.Add(fullName, type);
                }

                types.Add(type);
            }
            catch(Exception)
            {
                // TODO: Diagnostic
            }
        }

        var model = new DesignerModel(types, ns);
        var output = GenerateCode(context, model);

        context.AddSource("WebForms.Designer", output);
    }

    protected abstract string GenerateCode(SourceProductionContext context, DesignerModel output);
}
