using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlHead : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlHead()
        : base("head")
    {
    }

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();
        Page.Head = this;
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await base.RenderChildrenAsync(writer, token);

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderStartupHead(writer);
        }
    }
}
