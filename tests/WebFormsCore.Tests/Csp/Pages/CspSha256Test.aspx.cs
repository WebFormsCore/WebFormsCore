using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Csp.Pages;

public partial class CspShaTest : Page
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Csp.Enabled = true;

        // Uri is necessary for 'form.min.js'
        Csp.DefaultMode = CspMode.Sha256 | CspMode.Uri;
    }
}
