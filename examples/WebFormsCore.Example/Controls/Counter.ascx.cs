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
    [ViewState] private int _count;

    protected override void OnPreRender(EventArgs args)
    {
        tbPrefix.Text = tbPrefix.Text?.ToUpperInvariant();
        litValue.Text = $"Count: {tbPrefix.Text}{_count}";
    }

    protected async Task btnIncrement_OnClick(object? sender, EventArgs e)
    {
        _count++;
        await rptItems.AddAsync($"Item {_count}");
    }

    protected void rptItems_OnItemDataBound(object? sender, RepeaterItem e)
    {
        var item = (Literal?) e.FindControl("litItem");

        if (item != null)
        {
            item.Text = e.DataItem?.ToString();
        }
    }
}
