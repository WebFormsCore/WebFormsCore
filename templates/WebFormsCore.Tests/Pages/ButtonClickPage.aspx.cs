using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests1.Pages;

public partial class ButtonClickPage : Page
{
    protected Task btnClick_OnClick(object? sender, EventArgs e)
    {
        lblResult.Text = "Clicked!";
        return Task.CompletedTask;
    }
}
