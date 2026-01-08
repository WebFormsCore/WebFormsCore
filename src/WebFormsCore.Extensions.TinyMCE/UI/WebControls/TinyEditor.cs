using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Options;

namespace WebFormsCore.UI.WebControls;

public class TinyEditor : TextBox
{
    public TinyOptions? Options { get; set; }

    public TinyEditor()
    {
        TextMode = TextBoxMode.MultiLine;
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Page.Csp.StyleSrc.Add("'unsafe-inline'");
        Page.ClientScript.RegisterStartupStyleLink(typeof(TinyEditor), "Oxide", "https://cdnjs.cloudflare.com/ajax/libs/tinymce/6.7.0/skins/ui/oxide/skin.min.css");
        Page.ClientScript.RegisterStartupDeferStaticScript(typeof(TinyEditor), "/js/tiny.min.js", Resources.Script);
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var options = Options ?? Context.RequestServices.GetService<IOptions<TinyOptions>>()?.Value ?? TinyOptions.Default;
        var optionsJson = JsonSerializer.Serialize(options, JsonContext.Default.TinyOptions);

        writer.AddAttribute(HtmlTextWriterAttribute.Class, "js-tinymce");
        writer.AddAttribute(HtmlTextWriterAttribute.Style, $"visibility:hidden;height:{options.Height ?? 400}px;");
        if (!string.Equals(optionsJson, "{}", StringComparison.Ordinal))
        {
            writer.AddAttribute("data-options", optionsJson);
        }
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);

        await base.RenderAsync(writer, token);

        await writer.RenderEndTagAsync();
    }
}
