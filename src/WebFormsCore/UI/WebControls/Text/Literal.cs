using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class Literal : Control, ITextControl
{
    protected override bool EnableViewStateBag => false;

    [ViewState] public string? Text { get; set; }

    public override Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return string.IsNullOrEmpty(Text)
            ? Task.CompletedTask
            : writer.WriteAsync(Text);
    }

    string ITextControl.Text
    {
        get => Text ?? string.Empty;
        set => Text = value;
    }
}
