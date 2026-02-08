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

    [Theory, ClassData(typeof(BrowserData))]
    public async Task StronglyTypedMasterType(Browser type)
    {
        await using var result = await fixture.StartAsync<MasterTypeContentPage>(type);

        // Verify the strongly-typed Master property was used to set lblTitle
        var title = result.Control.lblTitle.FindBrowserElement();
        Assert.Equal("Default Title", title.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task DefaultContentRendersWhenNoContentProvided(Browser type)
    {
        await using var result = await fixture.StartAsync<DefaultContentPage>(type);

        // The "main" ContentPlaceHolder is not filled, so default content should render
        var content = result.QuerySelector("#content");
        Assert.NotNull(content);
        Assert.Contains("Default content", content.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task FindControlAndHeaderIntegration(Browser type)
    {
        await using var result = await fixture.StartAsync<FindControlPage>(type);

        // Verify master page properties accessible via @MasterType
        Assert.Equal("Default Title", result.Control.MasterLabelText);

        // Verify FindControl works across master/content boundary
        Assert.True(result.Control.FoundControlInMaster);

        // Verify Page.Header is set from master page's <head runat="server">
        Assert.True(result.Control.HasHeader);
    }
}
