using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Pages;

public partial class ClientIdPage : Page
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        serverId.ClientID = "clientId";
    }
}
