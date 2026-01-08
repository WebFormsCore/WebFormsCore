using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Csp.Pages;

public partial class CspNonceTest : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Csp.Enabled = true;
        Csp.DefaultMode = CspMode.Nonce;
    }
}
