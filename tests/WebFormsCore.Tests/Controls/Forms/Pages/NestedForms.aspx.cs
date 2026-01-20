using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Forms.Pages;

public partial class NestedForms : Page
{
    protected Task IncrementOuterCounter(Button sender, EventArgs e)
    {
        var currentValue = int.Parse(outerCounter.Text);
        outerCounter.Text = (currentValue + 1).ToString();
        return Task.CompletedTask;
    }

    protected Task IncrementInnerCounter(Button sender, EventArgs e)
    {
        var currentValue = int.Parse(innerCounter.Text);
        innerCounter.Text = (currentValue + 1).ToString();
        return Task.CompletedTask;
    }
}
