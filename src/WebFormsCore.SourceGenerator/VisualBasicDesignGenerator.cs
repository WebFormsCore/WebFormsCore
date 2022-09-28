using Microsoft.CodeAnalysis;
using Scriban;
using WebFormsCore.Nodes;

namespace WebFormsCore.SourceGenerator;

[Generator(LanguageNames.VisualBasic)]
public class VisualBasicDesignGenerator : DesignerGenerator
{
    protected override string GenerateCode(SourceProductionContext context, DesignerModel model)
    {
        const string templateFile = "Templates/vb-designer.scriban";
        var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);
        return template.Render(model, member => member.Name);
    }
}
