using WebFormsCore.Tests1.Pages;

namespace WebFormsCore.Tests1;

public class ButtonClickTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClickButton_UpdatesLabel(Browser type)
    {
        await using var result = await fixture.StartAsync<ButtonClickPage>(type);

        // Verify the label is empty before clicking
        Assert.Empty(result.Control.lblResult.Text);

        // Click the button and wait for the postback to complete
        await result.Control.btnClick.ClickAsync();

        // Verify the label text was updated by the server-side click handler
        Assert.Equal("Clicked!", result.Control.lblResult.FindBrowserElement().Text);
    }
}
