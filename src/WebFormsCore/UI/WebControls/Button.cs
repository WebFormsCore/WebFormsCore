using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public partial class Button : WebControl, IPostBackAsyncEventHandler
{
    public Button()
        : base(HtmlTextWriterTag.Input)
    {
    }

    public event AsyncEventHandler? Click;

    [ViewState] public string? Text { get; set; }

    public async Task RaisePostBackEventAsync(string? eventArgument)
    {
        await Click.InvokeAsync(this, EventArgs.Empty);
    }

    protected override async Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
        writer.AddAttribute(HtmlTextWriterAttribute.Value, Text);
        writer.AddAttribute("data-wfc-postback", UniqueID);
    }
}
