using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Checkboxes.Pages;

public partial class CheckboxAutoPostbackPage : Page
{
    protected Task checkbox_OnCheckedChanged(CheckBox sender, EventArgs e)
    {
        label.Text = sender.Checked ? "Checked" : "Unchecked";
        return Task.CompletedTask;
    }
}
