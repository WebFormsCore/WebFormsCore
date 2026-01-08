using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Callbacks.Pages;

public partial class CallbackPage : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

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
