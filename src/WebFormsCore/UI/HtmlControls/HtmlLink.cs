using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlLink : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlLink()
        : base("link")
    {
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        if (Attributes["rel"] == "stylesheet" &&
            Attributes.TryGetValue("href", out var href)
            && href != null
            && HtmlStyle.RenderStyles(this)
            && Uri.TryCreate(href, UriKind.Relative, out _))
        {
            Page.EarlyHints.AddStyle(href);
        }
    }

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!HtmlStyle.RenderStyles(this) && Attributes["rel"] == "stylesheet")
        {
            return default;
        }

        return base.RenderAsync(writer, token);
    }
}
