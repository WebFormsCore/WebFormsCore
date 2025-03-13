using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlScript : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlScript()
        : base("script")
    {
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        if (Attributes.TryGetValue("src", out var href)
            && href != null
            && RenderScripts(this)
            && Uri.TryCreate(href, UriKind.Relative, out _))
        {
            Page.EarlyHints.AddScript(href);
        }
    }

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!RenderScripts(this))
        {
            return default;
        }

        return base.RenderAsync(writer, token);
    }

    internal static bool RenderScripts(Control control)
    {
        var options = control.Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value;

        return !control.Page.IsPostBack || (options?.RenderScriptOnPostBack ?? false);
    }
}