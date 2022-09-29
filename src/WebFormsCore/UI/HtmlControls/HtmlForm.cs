using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlForm : HtmlContainerControl, INamingContainer, IStateContainer
{
    public HtmlForm()
        : base("form")
    {
    }

    protected override void OnInit(EventArgs args)
    {
        Page.Forms.Add(this);
    }

    protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        await writer.WriteAttributeAsync("method", "post");
        await writer.WriteAttributeAsync("id", ClientID);
        await writer.WriteAttributeAsync("data-wfc-form", null);
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        await writer.WriteAsync(@"<input type=""hidden"" name=""__FORM"" value=""");
        await writer.WriteAsync(UniqueID);
        await writer.WriteAsync(@"""/>");

        await writer.WriteAsync(@"<input type=""hidden"" name=""__FORMSTATE"" value=""");
        using (var viewState = viewStateManager.Write(this, out var length))
        {
            await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
        }
        await writer.WriteAsync(@"""/>");
    }
}
