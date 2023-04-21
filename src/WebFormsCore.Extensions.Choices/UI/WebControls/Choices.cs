namespace WebFormsCore.UI.WebControls;

public class Choices : Control
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Page.ClientScript.RegisterStartupStyleLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/styles/choices.min.css");
        Page.ClientScript.RegisterStartupScriptLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/scripts/choices.min.js");
        Page.ClientScript.RegisterStartupScript(typeof(Choices), "ChoicesStartup", """
            const choices = [];

            for (const input of document.querySelectorAll('.js-choice')) {
                choices.push({ input, instance: new Choices(input) });
            }

            document.addEventListener('wfc:addInputs', function (e) {
                for (const item of choices) {
                    e.detail.elements.push(item.input);
                    console.log(item.input);
                }
            });
            """);
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        writer.AddAttribute("data-wfc-ignore", "");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "js-choice");
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Input);
            await writer.RenderEndTagAsync();
        }
        await writer.RenderEndTagAsync();
    }
}
