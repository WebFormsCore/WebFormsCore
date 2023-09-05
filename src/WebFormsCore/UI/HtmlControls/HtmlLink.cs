using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlLink : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    private string? _nonce;
    
    public HtmlLink()
        : base("link")
    {
    }

    protected override Task OnPreRenderAsync(CancellationToken token)
    {
        if (Page.Csp.Enabled && Attributes["rel"] == "stylesheet")
        {
            if (Uri.TryCreate(Attributes["href"], UriKind.Absolute, out var href))
            {
                Page.Csp.StyleSrc.SourceList.Add($"{href.Scheme}://{href.Host}");
            }
            else
            {
                _nonce = Page.Csp.StyleSrc.GenerateNonce();
            }
        }

        return Task.CompletedTask;
    }

    protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);

        if (_nonce != null)
        {
            await writer.WriteAttributeAsync("nonce", _nonce);
        }
    }

    public override void ClearControl()
    {
        base.ClearControl();
        _nonce = null;
    }
}
