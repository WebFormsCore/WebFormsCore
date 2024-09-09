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

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        Page.Body ??= this;
    }

    protected override void OnUnload(EventArgs args)
    {
        base.OnUnload(args);

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

        if (!page.IsPostBack)
        {
            await page.ClientScript.RenderBodyStart(writer);
        }
    }

    internal static async Task RenderBodyEndAsync(Control control, HtmlTextWriter writer, CancellationToken token)
    {
        var page = control.Page;

        foreach (var renderer in control.Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderBodyAsync(page, writer, token);
        }

        if (!page.IsPostBack)
        {
            await page.ClientScript.RenderBodyEnd(writer);
        }

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