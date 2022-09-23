using System.Threading;
using System.Threading.Tasks;
using WebFormsCore;
using WebFormsCore.UI;

namespace WebForms.Example.Controls;

public partial class Counter : Control
{
    [ViewState] private int Count { get; set; }

    protected override ValueTask OnLoadAsync(CancellationToken token)
    {
        if (Page.IsPostBack)
        {
            Count++;
        }

        litValue.Text = $"Count: {Count}";

        return default;
    }
}
