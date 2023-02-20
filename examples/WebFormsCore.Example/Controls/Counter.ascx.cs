using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example.Controls;

public partial class Counter : Control
{
    [ViewState] protected ushort Count;

    protected override void OnPreRender(EventArgs args)
    {
        litCounter.Text = Count.ToString();
    }

    protected Task btnIncrement_OnClick(object? sender, EventArgs e)
    {
        Count++;
        return Task.CompletedTask;
    }
}
