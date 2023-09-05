using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlScript : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    private string? _nonce;

    public HtmlScript()
        : base("script")
    {
    }

    protected override Task OnPreRenderAsync(CancellationToken token)
    {
        if (Page.Csp.Enabled)
        {
            if (Uri.TryCreate(Attributes["src"], UriKind.Absolute, out var href))
            {
                Page.Csp.ScriptSrc.Add($"{href.Scheme}://{href.Host}");
            }
            else if (Page.Csp.ScriptSrc.Mode == CspMode.Sha256)
            {
                Page.Csp.ScriptSrc.AddInlineHash(InnerHtml);
            }
            else
            {
                _nonce = Page.Csp.ScriptSrc.GenerateNonce();
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
