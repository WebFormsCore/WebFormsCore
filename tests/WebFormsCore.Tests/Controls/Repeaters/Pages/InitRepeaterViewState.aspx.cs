using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters.Pages;

public partial class InitRepeaterViewState : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        lblViewState.Text = "Success";

        rptItems.DataSource = Enumerable.Range(1, 3);
        await rptItems.DataBindAsync(token);
    }

    protected Task rptItems_OnItemCreated(Repeater sender, RepeaterItemEventArgs e)
    {
        if (e.Item.DataItem is not int)
        {
            // Expected to be an int
            return Task.CompletedTask;
        }

        if (e.Item.FindControl<Label>("lblItem") is { } label)
        {
            label.Text = "Success";
        }

        return Task.CompletedTask;
    }
}
