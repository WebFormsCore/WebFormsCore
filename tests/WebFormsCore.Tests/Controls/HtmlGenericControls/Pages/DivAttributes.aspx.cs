using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

public partial class DivAttributes : Page
{
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        content.Style["color"] = "red";
        content.Style["font-size"] = "12px";
        content.Attributes["data-foo"] = "bar";
        content.Attributes["data-removed"] = "removed";
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (!IsPostBack)
        {
            content.Style["background-color"] = "blue";
            content.Style.Remove("font-size");
            content.Attributes["data-bar"] = "foo";
            content.Attributes.Remove("data-removed");
        }
    }

}
