using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Events;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlBody() : HtmlContainerControl("body")
{
    protected override bool GenerateAutomaticID => false;

    public string? LastViewState { get; set; }

    protected override void OnFrameworkInit()
    {
        Page.Body ??= this;

        base.OnFrameworkInit();
    }

    protected override async ValueTask OnUnloadAsync(CancellationToken token)
    {
        await base.OnUnloadAsync(token);

        if (Page.Body == this)
        {
            Page.Body = null;
        }
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await RenderBodyStartAsync(this, writer);

        await base.RenderChildrenAsync(writer, token);

        await RenderBodyEndAsync(this, writer, token);
    }

    internal static async Task RenderBodyStartAsync(Control control, HtmlTextWriter writer)
    {
        var page = control.Page;

        if (HtmlStyle.RenderStyles(control))
        {
            await page.ClientScript.RenderBodyStart(writer, ScriptType.Style);
        }

        if (HtmlScript.RenderScripts(control))
        {
            await page.ClientScript.RenderBodyStart(writer, ScriptType.Script);
        }
    }

    internal static async Task RenderBodyEndAsync(Control control, HtmlTextWriter writer, CancellationToken token)
    {
        var page = control.Page;

        foreach (var renderer in control.Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderBodyAsync(page, writer, token);
        }

        if (HtmlStyle.RenderStyles(control))
        {
            await page.ClientScript.RenderBodyEnd(writer, ScriptType.Style);
        }

        if (HtmlScript.RenderScripts(control))
        {
            await page.ClientScript.RenderBodyEnd(writer, ScriptType.Script);
        }

        await page.ClientScript.RenderCallbacksAndClear(writer);

        var viewStateManager = control.Context.RequestServices.GetRequiredService<IViewStateManager>();

        if (viewStateManager.EnableViewState && page is { EnableViewState: true, IsStreaming: false })
        {
            await writer.WriteAsync(@"<input id=""pagestate"" type=""hidden"" name=""wfcPageState"" value=""");

            using (var viewState = await viewStateManager.WriteAsync(control.Page, out var length))
            {
                await writer.WriteAsync(viewState.Memory.Slice(0, length));
            }

            await writer.WriteAsync(@"""/>");
        }
    }
}