using System;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example.Controls;

public partial class TodoList : Control
{
    protected async Task tbItem_OnEnterPressed(object? sender, EventArgs e)
    {
        if (tbItem.Text is {} value)
        {
            await rptItems.AddItemAsync(value);
            tbItem.Text = "";
        }
    }

    protected Task rptItems_OnItemDataBound(object? sender, RepeaterItemEventArgs<string> e)
    {
        if (e.Item.DataItem is not { } dataItem)
        {
            return Task.CompletedTask;
        }

        var controls = e.Item.FindControls<ItemControls>();

        controls.litValue.Text = dataItem;

        return Task.CompletedTask;
    }
}
