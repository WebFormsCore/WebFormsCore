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
            new() { Text = "Item 1" },
            new() { Text = "Item 2" },
            new() { Text = "Item 3" },
            new() { Text = "Item 4" },
            new() { Text = "Item 5" },
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

public class RepeaterItem
{
    public string Text { get; set; } = "";
}