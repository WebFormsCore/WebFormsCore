namespace WebFormsCore.Web;

public partial class Default : WebFormsCore.UI.Page
{
    protected async Task btnClick_Click(object? sender, EventArgs e)
    {
        litMessage.Text = "You clicked the button at " + DateTime.Now;
    }
}
