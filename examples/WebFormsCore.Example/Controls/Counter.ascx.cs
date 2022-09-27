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
        litValue.Text = $"Count: {tbPrefix.Text}{_count}";
    }

    protected async Task btnIncrement_OnClick(object? sender, EventArgs e)
    {
        _count++;
        await rptItems.AddAsync($"Item {_count}");
    }

    protected void rptItems_OnItemDataBound(object? sender, RepeaterItemEventArgs e)
    {
        var item = (Literal?) e.Item.FindControl("litItem");

        if (item != null)
        {
            item.Text = e.Item.DataItem?.ToString();
        }
    }
}
