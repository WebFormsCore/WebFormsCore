using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Pages;

public partial class TypedRepeater : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        items.DataSource = new List<RepeaterItem>
        {
            new() { Id = 1, Text = "Item 1" },
            new() { Id = 2, Text = "Item 2" },
            new() { Id = 3, Text = "Item 3" },
            new() { Id = 4, Text = "Item 4" },
            new() { Id = 5, Text = "Item 5" },
        };

        await items.DataBindAsync();
    }

    public Task items_OnItemDataBound(object? sender, RepeaterItemEventArgs e)
    {
        var item = (RepeaterItem)e.Item.DataItem!;

        if (e.Item.FindControl("item") is Literal text)
        {
            text.Text = item.Text;
        }

        return Task.CompletedTask;
    }
}