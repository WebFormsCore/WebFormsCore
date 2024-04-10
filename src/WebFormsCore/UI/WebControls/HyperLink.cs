using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class HyperLink() : WebControl(HtmlTextWriterTag.A)
{
    [ViewState] public string? Text { get; set; }

    [ViewState] public string? NavigateUrl { get; set; }

    [ViewState] public string? Target { get; set; }

    public override bool SupportsDisabledAttribute => false;

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        if (!string.IsNullOrEmpty(NavigateUrl))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, NavigateUrl);
        }

        if (!string.IsNullOrEmpty(Target))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Target, Target);
        }

        await base.AddAttributesToRender(writer, token);
    }

    protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasControls()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(Text);
    }
}
