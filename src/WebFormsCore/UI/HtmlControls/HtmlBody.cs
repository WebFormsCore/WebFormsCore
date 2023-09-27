using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Events;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlBody : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlBody()
        : base("body")
    {
    }

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        Page.Body ??= this;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnUnload(args);

        RegisterScript();
    }

    private void RegisterScript()
    {
        // TODO: Move this to a better place.
        var addScript = Context.RequestServices
            .GetService<IOptions<WebFormsCoreOptions>>()
            ?.Value
            ?.AddWebFormsCoreScript ?? true;

        if (addScript)
        {
            Page.ClientScript.RegisterStartupDeferStaticScript(typeof(Page), "/js/form.min.js", Resources.Script);
        }
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
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        foreach (var renderer in Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderBodyAsync(Page, writer, token);
        }

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderStartupBody(writer);
        }

        if (viewStateManager.EnableViewState && Page is { EnableViewState: true, IsStreaming: false })
        {
            await writer.WriteAsync(@"<input id=""pagestate"" type=""hidden"" name=""wfcPageState"" value=""");

            using (var viewState = await viewStateManager.WriteAsync(Page, out var length))
            {
                await writer.WriteAsync(viewState.Memory.Slice(0, length));
            }

            await writer.WriteAsync(@"""/>");
        }
    }
}