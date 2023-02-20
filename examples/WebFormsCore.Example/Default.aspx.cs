using System;
using System.Globalization;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int PostbackCount { get; set; }

    protected override void OnInit(EventArgs args)
    {
        Csp.Enabled = true;
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }

    protected Task OnClick(object? sender, EventArgs e)
    {
        litDateTime.Text = tbPrefix.Text + "" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
        return Task.CompletedTask;
    }
}
