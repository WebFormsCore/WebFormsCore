using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Events;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlHead : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlHead()
        : base("head")
    {
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        if (Page.Header == null)
        {
            Page.Header = this;
        }

        var hiddenClass = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value?.HiddenClass ?? "";

        Page.ClientScript.RegisterHeadScript(typeof(Page), "FormPostback", $$$"""WebFormsCore={hiddenClass:'{{{hiddenClass}}}',_:[],bind:function(a,b){this._.push([0,a,b])},bindValidator:function(a,b){this._.push([1,a,b])}};""");
    }

    protected override void OnUnload(EventArgs args)
    {
        base.OnUnload(args);

        if (Page.Header == this)
        {
            Page.Header = null;
        }
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await base.RenderChildrenAsync(writer, token);

        foreach (var renderer in Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderHeadAsync(Page, writer, token);
        }

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderStartupHead(writer);
        }
    }
}
