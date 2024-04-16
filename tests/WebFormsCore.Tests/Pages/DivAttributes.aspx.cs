using WebFormsCore.UI;

namespace WebFormsCore.Tests.Pages;

public partial class DivAttributes : Page
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        content.Style["color"] = "red";
        content.Attributes["data-foo"] = "bar";
    }
    
}
