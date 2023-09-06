using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public partial class Button : WebControl, IPostBackAsyncEventHandler
{
    public Button()
        : base(HtmlTextWriterTag.Button)
    {
    }

    public event AsyncEventHandler<Button, EventArgs>? Click;

    [ViewState] public AttributeCollection Style { get; set; } = new();

    [ViewState] public string? Text { get; set; }

    public async Task RaisePostBackEventAsync(string? eventArgument)
    {
        await Click.InvokeAsync(this, EventArgs.Empty);
    }

    protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasControls()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(Text, token);
    }

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        if (TagKey is HtmlTextWriterTag.Button or HtmlTextWriterTag.Input)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
        }

        writer.AddAttribute("data-wfc-postback", UniqueID);
    }
}
