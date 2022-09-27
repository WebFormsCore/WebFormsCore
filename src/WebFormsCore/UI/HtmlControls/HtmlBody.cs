using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlBody : HtmlContainerControl
{
    public static string Script { get; }

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

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        await writer.WriteAsync(@"<input id=""pagestate"" type=""hidden"" name=""__PAGESTATE"" value=""");
        using (var viewState = viewStateManager.Write(Page, out var length))
        {
            await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
        }
        await writer.WriteAsync(@"""/>");

        await writer.WriteAsync(@"<script>");
        await writer.WriteAsync(Script);
        await writer.WriteAsync(@"</script>");
    }
}
