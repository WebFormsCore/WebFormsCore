using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Forms.Pages;

public partial class TwoForms : Page
{
    protected Task IncrementCounter1(Button sender, EventArgs e)
    {
        var currentValue = int.Parse(counter1.Text);
        counter1.Text = (currentValue + 1).ToString();
        return Task.CompletedTask;
    }

    protected Task IncrementCounter2(Button sender, EventArgs e)
    {
        var currentValue = int.Parse(counter2.Text);
        counter2.Text = (currentValue + 1).ToString();
        return Task.CompletedTask;
    }
}
