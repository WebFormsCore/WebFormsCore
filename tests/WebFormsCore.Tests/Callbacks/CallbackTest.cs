using WebFormsCore.Tests.Callbacks.Pages;

namespace WebFormsCore.Tests.Callbacks;

public class CallbackTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task CallbackInvoked(Browser type)
    {
        await using var result = await fixture.StartAsync<CallbackPage>(type);

        Assert.Equal("Init", result.QuerySelectorRequired("#value").Text);

        await result.Control.btnSetValue.ClickAsync();

        Assert.Equal("Button", result.QuerySelectorRequired("#value").Text);
    }
}
