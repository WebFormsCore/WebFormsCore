using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public class Button : BaseButton<Button>
{
    public Button()
        : base(HtmlTextWriterTag.Button)
    {
    }
}

public partial class BaseButton<TSelf> : WebControl, IButtonControl, IPostBackAsyncEventHandler
    where TSelf : BaseButton<TSelf>
{
    private AsyncEventHandler? _click;

    protected BaseButton(HtmlTextWriterTag tag)
        : base(tag)
    {
    }

    public event AsyncEventHandler<TSelf, EventArgs>? Click;

    public event AsyncEventHandler<TSelf, CommandEventArgs>? Command;

    [ViewState] public string? Text { get; set; }

    [ViewState] public bool CausesValidation { get; set; } = true;

    [ViewState] public string? CommandArgument { get; set; }

    [ViewState] public string? CommandName { get; set; }

    [ViewState] public string ValidationGroup { get; set; } = "";

    public async Task RaisePostBackEventAsync(string? eventArgument)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (CausesValidation && !await Page.ValidateAsync(ValidationGroup))
        {
            return;
        }

        await Click.InvokeAsync((TSelf)this, EventArgs.Empty);
        await _click.InvokeAsync(this, EventArgs.Empty);

        var args = new CommandEventArgs(CommandName, CommandArgument);
        await Command.InvokeAsync((TSelf)this, args);
        await RaiseBubbleEventAsync(this, args);
    }

    protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasControls()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(Text);
    }

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        AddButtonAttributesToRender(writer);

        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute("data-wfc-postback", UniqueID);

        if (CausesValidation)
        {
            writer.AddAttribute("data-wfc-validate", ValidationGroup == "" ? null : ValidationGroup);
        }
    }

    protected virtual void AddButtonAttributesToRender(HtmlTextWriter writer)
    {
        if (TagKey is HtmlTextWriterTag.Button or HtmlTextWriterTag.Input)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
        }
    }

    event AsyncEventHandler? IButtonControl.Click
    {
        add => _click += value;
        remove => _click -= value;
    }

    public override void ClearControl()
    {
        base.ClearControl();

        Text = null;
        CausesValidation = true;
        CommandArgument = null;
        CommandName = null;
        ValidationGroup = "";
        Command = null;
        Click = null;
        _click = null;
    }
}
