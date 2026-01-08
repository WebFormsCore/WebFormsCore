using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.DropDown.Pages;

public partial class DropDownPage : Page
{
    public int EventCount;

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        ddl.Items.Add(new ListItem("Select an option", ""));
        ddl.Items.Add(new ListItem("Option 1", "1"));
        ddl.Items.Add(new ListItem("Option 2", "2"));
        ddl.Items.Add(new ListItem("Option 3", "3"));
    }

    protected Task ddl_OnSelectedIndexChanged(DropDownList sender, EventArgs e)
    {
        Interlocked.Increment(ref EventCount);
        return Task.CompletedTask;
    }
}
