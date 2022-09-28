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

        if (!analyzer.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns))
        {
            ns = compilation.AssemblyName;
        }

        var (webConfigPath, webConfigText) = files.FirstOrDefault(x => x.Path.EndsWith("web.config", StringComparison.OrdinalIgnoreCase));

        var namespaces = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(webConfigText))
        {
            try
            {
                var controls = XElement.Parse(webConfigText)
                    .Descendants("system.web").FirstOrDefault()
                    ?.Descendants("pages").FirstOrDefault()
                    ?.Descendants("controls").FirstOrDefault();

                if (controls != null)
                {
                    foreach (var add in controls.Descendants("add"))
                    {
                        var tagPrefix = add.Attribute("tagPrefix")?.Value;
                        var namespaceName = add.Attribute("namespace")?.Value;

                        if (tagPrefix != null && namespaceName != null)
                        {
                            namespaces.Add(new KeyValuePair<string, string>(tagPrefix, namespaceName));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Diagnostic
            }
        }

        foreach (var (fullPath, text) in files)
        {
            if (fullPath == webConfigPath) continue;

            try
            {
                var path = fullPath;

                if (directory != null && path.StartsWith(directory))
                {
                    path = path.Substring(directory.Length + 1);
                }

                if (!visited.Add(path)) continue;
                if (RootNode.Parse(compilation, path, text, ns, namespaces) is not { } type) continue;
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
            catch(Exception e)
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
