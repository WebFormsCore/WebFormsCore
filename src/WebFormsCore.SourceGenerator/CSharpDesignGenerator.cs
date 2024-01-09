using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Scriban;
using WebFormsCore.Nodes;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class CSharpDesignGenerator : DesignerGenerator
{
    protected override string? GetGenerateAssemblyTypeProvider(ImmutableArray<ControlType> source)
    {
        if (source.IsEmpty)
        {
            return null;
        }

        var rootNamespace = source[0].RootNamespace;

        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(rootNamespace))
        {
            builder.Append("namespace ").Append(rootNamespace).AppendLine();
            builder.AppendLine("{");
            builder.AppendLine();
        }

        builder.AppendLine("internal class AssemblyControlTypeProvider : WebFormsCore.IControlTypeProvider");
        builder.AppendLine("{");

        builder.AppendLine("    public System.Collections.Generic.Dictionary<string, System.Type> GetTypes()");
        builder.AppendLine("    {");

        builder.AppendLine("        return new System.Collections.Generic.Dictionary<string, System.Type>");
        builder.AppendLine("        {");

        foreach (var controlType in source)
        {
            builder.Append("            { \"").Append(controlType.RelativePath).Append("\", typeof(").Append(controlType.CompiledViewType.Replace('+', '.')).AppendLine(") },");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(rootNamespace))
        {
            builder.AppendLine();
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    protected override string GenerateCode(DesignerModel model)
    {
        const string templateFile = "Templates/designer.scriban";
        var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);
        return template.Render(model, member => member.Name);
    }
}
