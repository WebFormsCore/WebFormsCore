using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Pages;

public partial class ClickTest : Page
{
    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        if (!IsPostBack)
        {
            btnSetResult.CommandArgument = "Success";
        }

        await base.OnLoadAsync(token);
    }

    protected Task btnSetResult_OnClick(Button sender, EventArgs e)
    {
        lblResult.Text = sender.CommandArgument ?? "No argument";
        return Task.CompletedTask;
    }
}
