using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Forms.Pages;

public partial class NoForm : Page
{
    protected Task IncrementCounter(Button sender, EventArgs e)
    {
        var currentValue = int.Parse(counter.Text);
        counter.Text = (currentValue + 1).ToString();
        return Task.CompletedTask;
    }
}
