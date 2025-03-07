using WebFormsCore.Tests.Controls.TextBoxes.Pages;

namespace WebFormsCore.Tests.Controls.TextBoxes;

public class TextBoxTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task Postback(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Initial value is empty
        Assert.Equal("", result.Control.textBox.FindBrowserElement().Value);
        await result.Control.button.ClickAsync();
        Assert.Equal("", result.Control.textBox.FindBrowserElement().Value);

        // Change the value
        await result.Control.textBox.ClearAsync();
        await result.Control.textBox.TypeAsync("Changed");

        Assert.Equal("Changed", result.Control.textBox.FindBrowserElement().Value);
        await result.Control.button.ClickAsync();
        Assert.Equal("Changed", result.Control.textBox.FindBrowserElement().Value);

        // Clear the value
        await result.Control.textBox.ClearAsync();

        Assert.Equal("", result.Control.textBox.FindBrowserElement().Value);
        await result.Control.button.ClickAsync();
        Assert.Equal("", result.Control.textBox.FindBrowserElement().Value);
    }
}
