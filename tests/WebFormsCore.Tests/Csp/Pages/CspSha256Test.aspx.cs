using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Csp.Pages;

public partial class CspShaTest : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Csp.Enabled = true;

        // Uri is necessary for 'form.min.js'
        Csp.DefaultMode = CspMode.Sha256 | CspMode.Uri;
    }
}
