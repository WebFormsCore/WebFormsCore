using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class TabsTest : Page
{
    protected async Task OnLazyTabContentLoaded(Tab sender, EventArgs e)
    {
        lblResult.Text = "Hello world! This content was loaded on demand.";
        await Task.Delay(2000);
    }

    private Task OnEventTabChanged(TabControl sender, EventArgs e)
    {
        lblEventLog.Text = $"ActiveTabChanged fired! New index: {sender.ActiveTabIndex}";
        return Task.CompletedTask;
    }
}
