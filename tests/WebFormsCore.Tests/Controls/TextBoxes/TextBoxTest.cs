using WebFormsCore.Tests.Controls.TextBoxes.Pages;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

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

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearFromServer(Browser type)
    {
        await using var result = await fixture.StartAsync<ClearTextBoxPage>(type);

        // Change the value
        await result.Control.textBox.TypeAsync("Hello");
        await result.Control.txtMulti.TypeAsync("World");
        await result.Control.btnPostback.ClickAsync();
        Assert.Equal("Hello", result.Control.textBox.FindBrowserElement().Value);
        Assert.Equal("World", result.Control.txtMulti.FindBrowserElement().Value);

        // Clear the value from server
        await result.Control.btnClear.ClickAsync();
        Assert.Equal("", result.Control.textBox.FindBrowserElement().Value);
        Assert.Equal("", result.Control.txtMulti.FindBrowserElement().Value);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TestMode(Browser type)
    {
        await using var result = await fixture.StartAsync(type, async control =>
        {
            var textBox = new TextBox
            {
                ID = "textBox",
                TextMode = TextBoxMode.Password
            };

            await control.Controls.AddAsync(textBox);

            return textBox;
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("password", await textBoxElement.GetAttributeAsync("type"));
    }
}
