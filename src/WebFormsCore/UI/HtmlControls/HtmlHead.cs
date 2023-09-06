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

    protected override void AfterAddedToParent()
    {
        base.AfterAddedToParent();
        Page.Header = this;
    }

    protected override void BeforeRemovedFromParent()
    {
        base.BeforeRemovedFromParent();
        Page.Header = null;
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await base.RenderChildrenAsync(writer, token);

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderStartupHead(writer);
        }
    }
}
