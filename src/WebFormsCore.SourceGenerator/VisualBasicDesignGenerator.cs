using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Scriban;
using WebFormsCore.Nodes;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

[Generator(LanguageNames.VisualBasic)]
public class VisualBasicDesignGenerator : DesignerGenerator
{
    protected override string? GetGenerateAssemblyTypeProvider(ImmutableArray<ControlType> source,
        string? rootNamespace)
    {
        return null;
    }

    protected override string GenerateCode(DesignerModel model)
    {
        const string templateFile = "Templates/vb-designer.scriban";
        var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);
        return template.Render(model, member => member.Name);
    }
}
