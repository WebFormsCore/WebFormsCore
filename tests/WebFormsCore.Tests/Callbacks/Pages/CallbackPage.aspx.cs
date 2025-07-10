using WebFormsCore.Security;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Callbacks.Pages;

public partial class CallbackPage : Page
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        if (!IsPostBack)
        {
            ClientScript.InvokeCallback("setValue", "Init");
        }
    }

    protected Task btnSetValue_Click(LinkButton sender, EventArgs e)
    {
        ClientScript.InvokeCallback("setValue", "Button");
        return Task.CompletedTask;
    }
}
