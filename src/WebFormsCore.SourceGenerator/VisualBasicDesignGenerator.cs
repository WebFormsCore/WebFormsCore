using Microsoft.CodeAnalysis;
using Scriban;
using WebFormsCore.Nodes;

namespace WebFormsCore.SourceGenerator;

[Generator(LanguageNames.VisualBasic)]
public class VisualBasicDesignGenerator : DesignerGenerator
{
    protected override void AddSource(SourceProductionContext context, DesignerModel model)
    {
        const string templateFile = "Templates/vb-designer.scriban";
        var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);
        var output = template.Render(model, member => member.Name);

        context.AddSource("WebForms.Designer", output);
    }
}
