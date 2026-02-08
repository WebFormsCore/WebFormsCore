using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.MasterPages.Pages;

public partial class MasterPageContentPage : Page
{
    protected Task btnPostback_OnClick(Button sender, EventArgs e)
    {
        lblResult.Text = "Postback success";
        return Task.CompletedTask;
    }
}
