using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Security;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlBody : HtmlContainerControl
{
    private static readonly string Script;

    static HtmlBody()
    {
        using var resource = typeof(HtmlBody).Assembly.GetManifestResourceStream("WebFormsCore.Scripts.form.min.js");
        using var reader = new StreamReader(resource!);
        Script = reader.ReadToEnd();
    }

    public HtmlBody()
        : base("body")
    {
    }

    protected override void OnInit(EventArgs args)
    {
        Page.ClientScript.RegisterStartupScript(typeof(Page), "FormPostback", Script, true);
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        if (viewStateManager.EnableViewState)
        {
            await writer.WriteAsync(@"<input id=""pagestate"" type=""hidden"" name=""__PAGESTATE"" value=""");
            using (var viewState = viewStateManager.Write(Page, out var length))
            {
                await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
            }

            await writer.WriteAsync(@"""/>");

            if (!Page.IsPostBack)
            {
                await Page.ClientScript.RenderStartupScripts(writer);
            }
        }
    }
}
