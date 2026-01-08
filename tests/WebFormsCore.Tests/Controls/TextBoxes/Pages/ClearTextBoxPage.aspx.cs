using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.TextBoxes.Pages;

public partial class ClearTextBoxPage : Page
{
    protected void btnClear_Click(object sender, EventArgs e)
    {
        textBox.Text = "";
        txtMulti.Text = "";
    }
}
