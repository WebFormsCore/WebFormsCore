using WebFormsCore.Tests.Csp.Pages;

namespace WebFormsCore.Tests.Csp;

public class CspTest
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task InlineScript_Nonce(Browser type)
    {
        await using var result = await StartAsync<CspNonceTest>(type);

        var csp = result.Control.Context.Response.Headers["Content-Security-Policy"].ToString();

        Assert.Contains("'nonce-", csp);
        Assert.Equal("Success", result.QuerySelectorRequired("#result").Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task InlineScript_Sha256(Browser type)
    {
        await using var result = await StartAsync<CspShaTest>(type);

        var csp = result.Control.Context.Response.Headers["Content-Security-Policy"].ToString();

        Assert.Contains("'sha256-", csp);
        Assert.Equal("Success", result.QuerySelectorRequired("#result").Text);
    }
}
