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
            await rptItems.AddAsync(value);
            tbItem.Text = "";
        }
    }

    protected Task rptItems_OnItemDataBound(object? sender, RepeaterItemEventArgs e)
    {
        if (e.Item.FindControl("litValue") is Literal litValue)
        {
            litValue.Text = e.Item.DataItem?.ToString();
        }

        return Task.CompletedTask;
    }
}
