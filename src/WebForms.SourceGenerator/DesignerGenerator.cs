using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WebFormsCore;
using WebFormsCore.Nodes;

namespace WebForms.SourceGenerator;

public abstract class DesignerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(a => a.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ||
                        a.Path.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
            .Select((a, c) => (a.Path, a.GetText(c)!.ToString().ReplaceLineEndings("\n")));

        var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());
        var options = context.AnalyzerConfigOptionsProvider.Combine(compilationAndFiles);

        context.RegisterSourceOutput(options, Generate);
    }
    
    public void Generate(SourceProductionContext context, (AnalyzerConfigOptionsProvider Left, (Compilation Left, ImmutableArray<(string Path, string)> Right) Right) sourceContext)
    {
        var (analyzer, (compilation, files)) = sourceContext;
        var types = new List<RootNode>();
        var visited = new HashSet<string>();

        if (!analyzer.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var directory))
        {
            var mainSyntaxTree = compilation.SyntaxTrees.First(x => x.HasCompilationUnitRoot);
            directory = Path.GetDirectoryName(mainSyntaxTree.FilePath);
        }

        if (!analyzer.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns))
        {
            ns = compilation.AssemblyName;
        }

        foreach (var (fullPath, text) in files)
        {
            var path = fullPath;

            if (directory != null && path.StartsWith(directory))
            {
                path = path.Substring(directory.Length + 1);
            }

            if (!visited.Add(path)) continue;

            if (RootNode.Parse(compilation, path, text, ns) is {} type)
            {
                types.Add(type);
            }
        }

        var model = new DesignerModel(types, ns);

        AddSource(context, model);
    }

    protected abstract void AddSource(SourceProductionContext context, DesignerModel output);
}
