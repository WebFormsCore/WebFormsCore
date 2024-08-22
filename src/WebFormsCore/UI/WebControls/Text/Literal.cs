using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.WebControls;

public partial class Literal : Control, ITextControl
{
    [ViewState] private LiteralMode? _mode;

    protected override bool EnableViewStateBag => false;

    [ViewState] public string? Text { get; set; }

    public LiteralMode Mode
    {
        get => _mode ??= Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value.DefaultLiteralMode ?? LiteralMode.Encode;
        set => _mode = value;
    }

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (string.IsNullOrEmpty(Text))
            return default;

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
