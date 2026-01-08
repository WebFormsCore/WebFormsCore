using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Interceptors.Pages;

public partial class RequestInterceptor : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        lblHeaderXTest.Text = Request.Headers["X-Test"].ToString();
    }
}
