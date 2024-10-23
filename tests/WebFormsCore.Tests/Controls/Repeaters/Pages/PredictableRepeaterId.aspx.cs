using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters.Pages;

public partial class PredictableRepeaterId : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        items.DataSource = Enumerable.Range(0, 1);
        await items.DataBindAsync(token);
    }

    public Task items_OnItemDataBound(object? sender, RepeaterItemEventArgs e)
    {
        if (e.Item.FindControl("lbl") is Label lbl)
        {
            lbl.Text = lbl.ClientID ?? "";
        }

        return Task.CompletedTask;
    }
}
