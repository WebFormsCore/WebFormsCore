using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Events;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlHead() : HtmlContainerControl("head")
{
    protected override bool GenerateAutomaticID => false;

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        Page.Header ??= this;
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
        await RenderHeadStartAsync(this, writer);
        await base.RenderChildrenAsync(writer, token);
        await RenderHeadEndAsync(this, writer, token);
    }

    internal static async Task RenderHeadStartAsync(Control control, HtmlTextWriter writer)
    {
        var page = control.Page;

        if (!page.IsPostBack)
        {
            await page.ClientScript.RenderHeadStart(writer);
        }
    }

    internal static async Task RenderHeadEndAsync(Control control, HtmlTextWriter writer, CancellationToken token)
    {
        var page = control.Page;

        foreach (var renderer in control.Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderHeadAsync(page, writer, token);
        }

        if (!page.IsPostBack)
        {
            await page.ClientScript.RenderHeadEnd(writer);
        }
    }

}
