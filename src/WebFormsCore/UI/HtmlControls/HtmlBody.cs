using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlBody : HtmlContainerControl
{
    public static readonly string Script;

    static HtmlBody()
    {
        using var resource = typeof(HtmlBody).Assembly.GetManifestResourceStream("WebFormsCore.Scripts.form.min.js");
        using var reader = new StreamReader(resource!);
        Script = reader.ReadToEnd();
    }

    protected override bool GenerateAutomaticID => false;

    public HtmlBody()
        : base("body")
    {
    }

    protected override void AfterAddedToParent()
    {
        base.AfterAddedToParent();
        Page.Body = this;
    }

    protected override void BeforeRemovedFromParent()
    {
        base.BeforeRemovedFromParent();
        Page.Body = null;
    }

    protected override void OnInit(EventArgs args)
    {
        // TODO: Move this to a better place.
        Page.ClientScript.RegisterStartupScript(typeof(Page), "FormPostback", Script, true);
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        foreach (var control in Page.BodyControls)
        {
            await control.RenderInBodyAsync(writer, token);
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

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderStartupBody(writer);
        }
    }
}
