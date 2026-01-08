using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

public partial class DivAttributes : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        content.Style["color"] = "red";
        content.Style["font-size"] = "12px";
        content.Attributes["data-foo"] = "bar";
        content.Attributes["data-removed"] = "removed";
    }


    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        if (!IsPostBack)
        {
            content.Style["background-color"] = "blue";
            content.Style.Remove("font-size");
            content.Attributes["data-bar"] = "foo";
            content.Attributes.Remove("data-removed");
        }
    }

}
