using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlForm : HtmlContainerControl, INamingContainer
{
    public static string Script { get; }

    static HtmlForm()
    {
        using var resource = typeof(HtmlForm).Assembly.GetManifestResourceStream("WebFormsCore.Scripts.form.min.js");
        using var reader = new StreamReader(resource!);
        Script = reader.ReadToEnd();
    }

    public HtmlForm()
        : base("form")
    {
    }

    public bool Global { get; set; }

    protected override async Task OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Page.Forms.Add(this);
    }

    internal virtual Control ViewStateOwner => Global ? Page : this;

    protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        await writer.WriteAttributeAsync("data-wfc-form", Global ? "global" : "scope");
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        await writer.WriteAsync(@"<input type=""hidden"" name=""__FORM"" value=""");
        await writer.WriteAsync(UniqueID);
        await writer.WriteAsync(@"""/>");


        using var viewState = viewStateManager.Write(this, out var length);

        await writer.WriteAsync(@"<input type=""hidden"" name=""__VIEWSTATE"" value=""");
        await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
        await writer.WriteAsync(@"""/>");

        if (Context.Items["FormScript"] == null)
        {
            Context.Items["FormScript"] = true;
            await writer.WriteAsync(@"<script>");
            await writer.WriteAsync(Script);
            await writer.WriteAsync(@"</script>");
        }
    }
}
