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

    protected override void OnFrameworkInit()
    {
        Page.Header ??= this;

        base.OnFrameworkInit();
    }

    protected override async ValueTask OnUnloadAsync(CancellationToken token)
    {
        await base.OnUnloadAsync(token);

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

        if (HtmlStyle.RenderStyles(control))
        {
            await page.ClientScript.RenderHeadStart(writer, ScriptType.Style);
        }

        if (HtmlScript.RenderScripts(control))
        {
            await page.ClientScript.RenderHeadStart(writer, ScriptType.Script);
        }
    }

    internal static async Task RenderHeadEndAsync(Control control, HtmlTextWriter writer, CancellationToken token)
    {
        var page = control.Page;

        foreach (var renderer in control.Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderHeadAsync(page, writer, token);
        }

        if (HtmlStyle.RenderStyles(control))
        {
            await page.ClientScript.RenderHeadEnd(writer, ScriptType.Style);
        }

        if (HtmlScript.RenderScripts(control))
        {
            await page.ClientScript.RenderHeadEnd(writer, ScriptType.Script);
        }
    }

}
