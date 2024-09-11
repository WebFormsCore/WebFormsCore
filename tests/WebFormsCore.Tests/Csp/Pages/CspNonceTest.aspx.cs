using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Csp.Pages;

public partial class CspNonceTest : Page
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Csp.Enabled = true;
        Csp.DefaultMode = CspMode.Nonce;
    }
}
