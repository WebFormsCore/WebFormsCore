using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Interceptors.Pages;

public partial class RequestInterceptor : Page
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        lblHeaderXTest.Text = Request.Headers["X-Test"].ToString();
    }
}
