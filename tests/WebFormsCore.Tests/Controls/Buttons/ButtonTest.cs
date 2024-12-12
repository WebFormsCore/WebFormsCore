using WebFormsCore.Tests.Controls.Buttons.Pages;

namespace WebFormsCore.Tests.Controls.Buttons;

public class ButtonTest(SeleniumFixture fixture) : IClassFixture<SeleniumFixture>
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task Click(Browser type)
    {
        await using var result = await fixture.StartAsync<ClickTest>(type);

        Assert.Empty(result.Control.lblResult.Text);

        await result.Control.btnSetResult.ClickAsync();

        Assert.Equal("Success", result.Control.lblResult.FindBrowserElement().Text);
    }
}
