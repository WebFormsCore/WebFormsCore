using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class Literal : Control, ITextControl
{
    protected override bool EnableViewStateBag => false;

    [ViewState] public string? Text { get; set; }

    [ViewState] public LiteralMode Mode { get; set; } = LiteralMode.Transform;

    public override Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (string.IsNullOrEmpty(Text))
            return Task.CompletedTask;

        if (Mode == LiteralMode.Encode)
            return writer.WriteEncodedTextAsync(Text);

        return writer.WriteAsync(Text);
    }

    string ITextControl.Text
    {
        get => Text ?? string.Empty;
        set => Text = value;
    }

    public override void ClearControl()
    {
        base.ClearControl();

        Text = null;
        Mode = LiteralMode.Transform;
    }
}
