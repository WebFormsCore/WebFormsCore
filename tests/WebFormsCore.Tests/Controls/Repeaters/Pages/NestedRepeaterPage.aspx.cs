using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters.Pages;

public partial class NestedRepeaterPage : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        a.DataSource = new[] { 1, 2 };
        await a.DataBindAsync(token);
    }

    protected async Task a_OnItemCreated(Repeater sender, RepeaterItemEventArgs e)
    {
        if (e.Item.DataItem is not int item)
        {
            return;
        }

        if (e.Item.FindControl<Repeater>("b") is { } b)
        {
            b.DataSource = new[]
            {
                (item, 1),
                (item, 2)
            };
            await b.DataBindAsync();
        }
    }

    protected Task b_OnItemCreated(Repeater sender, RepeaterItemEventArgs e)
    {
        if (e.Item.DataItem is not (int n1, int n2))
        {
            return Task.CompletedTask;
        }

        if (e.Item.FindControl<Label>("lbl") is { } lbl)
        {
            lbl.Text = $"{n1} - {n2}";
        }

        return Task.CompletedTask;
    }
}