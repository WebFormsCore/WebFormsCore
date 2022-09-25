using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public class Button : HtmlGenericControl
{
    public Button()
        : base("button")
    {
    }

    public event AsyncEventHandler? Click;

    protected override async Task OnPostbackAsync(CancellationToken token)
    {
        await base.OnPostbackAsync(token);

        if (Context.Request.Form["__EVENTTARGET"] == UniqueID)
        {
            await Click.InvokeAsync(this, EventArgs.Empty);
        }
    }

    protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        await writer.WriteAttributeAsync("data-wfc-postback", UniqueID);
    }
}
