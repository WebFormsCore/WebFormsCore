using WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls;

public class HtmlGenericControlTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task PageWithControlAndAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync<DivAttributes>(type);

        var element = result.Control.content.FindBrowserElement();

        Assert.Equal("color: red;", await element.GetAttributeAsync("style"));
        Assert.Equal("bar", await element.GetAttributeAsync("data-foo"));
    }
}
