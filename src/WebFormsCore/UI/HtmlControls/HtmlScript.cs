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