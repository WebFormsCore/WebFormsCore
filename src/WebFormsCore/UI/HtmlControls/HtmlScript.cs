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
        var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value;

        if (Page.IsPostBack && !(options?.RenderScriptOnPostBack ?? false))
        {
            return default;
        }

        return base.RenderAsync(writer, token);
    }
}