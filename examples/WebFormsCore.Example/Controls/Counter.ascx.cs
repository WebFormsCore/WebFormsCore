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
    [ViewState] private ushort _count;

    protected override void OnPreRender(EventArgs args)
    {
        litCounter.Text = _count.ToString();
    }

    protected Task btnIncrement_OnClick(object? sender, EventArgs e)
    {
        _count++;
        return Task.CompletedTask;
    }
}
