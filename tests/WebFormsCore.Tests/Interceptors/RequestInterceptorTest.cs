using WebFormsCore.Tests.Interceptors.Pages;

namespace WebFormsCore.Tests.Interceptors;

public class RequestInterceptorTest
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task AddHeader(Browser type)
    {
        await using var result = await StartAsync<RequestInterceptor>(type);

        await result.Control.btnSubmit.ClickAsync();

        Assert.Equal("Success", result.Control.Context.Request.Headers["X-Test"].ToString());
        Assert.Equal("Success", result.Control.lblHeaderXTest.Text);
    }
}
