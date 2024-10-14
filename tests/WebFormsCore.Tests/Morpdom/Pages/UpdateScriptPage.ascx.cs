using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Tests.Morphdom.Pages;

public partial class UpdateScriptPage : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        if (IsPostBack)
        {
            phScript.Visible = true;
        }

        await base.OnInitAsync(token);
    }
}
