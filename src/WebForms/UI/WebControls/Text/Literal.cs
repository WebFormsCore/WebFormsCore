using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class Literal : Control
{
    protected override bool EnableViewStateBag => false;

    [ViewState] public string Text { get; set; } = "";

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return new ValueTask(writer.WriteAsync(Text));
    }
}
