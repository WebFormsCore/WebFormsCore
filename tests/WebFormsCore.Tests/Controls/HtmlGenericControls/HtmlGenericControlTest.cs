using WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls;

public class HtmlGenericControlTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task PageWithControlAndAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync<DivAttributes>(type);

        // Validate initial state
        var element = result.Control.content.FindBrowserElement();

        Assert.Equal("color: red; background-color: blue;", await element.GetAttributeAsync("style"));
        Assert.Equal("bar", await element.GetAttributeAsync("data-foo"));
        Assert.Equal("foo", await element.GetAttributeAsync("data-bar"));
        Assert.Null(await element.GetAttributeAsync("data-removed"));

        // Validate view state
        await result.Control.btnSubmit.ClickAsync();
        element = result.Control.content.FindBrowserElement();

        Assert.Equal("color: red; background-color: blue;", await element.GetAttributeAsync("style"));
        Assert.Equal("bar", await element.GetAttributeAsync("data-foo"));
        Assert.Equal("foo", await element.GetAttributeAsync("data-bar"));
        Assert.Null(await element.GetAttributeAsync("data-removed"));
    }
}
