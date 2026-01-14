using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters.Pages;

public partial class RepeaterTemplatePage : Page
{
    public Repeater<string> templateRepeater = null!;

    protected override async ValueTask OnLoadAsync(CancellationToken ct)
    {
        await base.OnLoadAsync(ct);

        if (!IsPostBack)
        {
            templateRepeater.DataSource = new[] { "Item 1", "Item 2", "Item 3" };
            await templateRepeater.DataBindAsync();
        }
    }
}
