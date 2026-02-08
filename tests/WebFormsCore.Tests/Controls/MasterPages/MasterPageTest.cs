using WebFormsCore.Tests.Controls.MasterPages.Pages;

namespace WebFormsCore.Tests.Controls.MasterPages;

public class MasterPageTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ContentRendersInMasterPage(Browser type)
    {
        await using var result = await fixture.StartAsync<MasterPageContentPage>(type);

        // Verify master page structure is rendered
        var header = result.QuerySelector("#header");
        Assert.NotNull(header);

        var footer = result.QuerySelector("#footer");
        Assert.NotNull(footer);
        Assert.Equal("Footer", footer.Text);

        // Verify content from the content page is rendered in the correct placeholder
        var message = result.Control.lblMessage.FindBrowserElement();
        Assert.Equal("Hello from content page", message.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackThroughMasterPage(Browser type)
    {
        await using var result = await fixture.StartAsync<MasterPageContentPage>(type);

        // Initial state
        Assert.Equal("", result.Control.lblResult.FindBrowserElement().Text);

        // Click the button
        await result.Control.btnPostback.ClickAsync();

        // Verify postback result
        Assert.Equal("Postback success", result.Control.lblResult.FindBrowserElement().Text);
    }
}
