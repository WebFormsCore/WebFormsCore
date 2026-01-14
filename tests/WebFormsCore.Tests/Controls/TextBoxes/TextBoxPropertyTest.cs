using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.TextBoxes;

public class TextBoxPropertyTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "tb",
            MaxLength = 10,
            ReadOnly = true,
            AutoCompleteType = AutoCompleteType.Disabled,
            Columns = 30,
            Rows = 5,
            TextMode = TextBoxMode.MultiLine,
            Wrap = false
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("textarea", await result.Browser.ExecuteScriptAsync($"return document.getElementById('{result.State.ClientID}').tagName.toLowerCase();"));
        Assert.Equal("10", await element.GetAttributeAsync("maxlength"));
        Assert.Equal("true", await element.GetAttributeAsync("readonly"));
        Assert.Equal("off", await element.GetAttributeAsync("autocomplete"));
        Assert.Equal("30", await element.GetAttributeAsync("cols"));
        Assert.Equal("5", await element.GetAttributeAsync("rows"));
        Assert.Equal("off", await element.GetAttributeAsync("wrap"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task SingleLineAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "tb",
            Columns = 20,
            TextMode = TextBoxMode.SingleLine
        });

        var element = result.State.FindBrowserElement();
        Assert.Equal("input", await result.Browser.ExecuteScriptAsync($"return document.getElementById('{result.State.ClientID}').tagName.toLowerCase();"));
        Assert.Equal("20", await element.GetAttributeAsync("size"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ReadOnly_ShouldNotUpdateValue(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var textBox = new TextBox
            {
                ID = "tb",
                ReadOnly = true,
                Text = "Initial"
            };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [textBox, btn] };

            container.Controls = [panel];

            return (panel, textBox, btn);
        });

        // Try to change value via JS since it's readonly
        await result.Browser.ExecuteScriptAsync($"document.getElementById('{result.State.textBox.ClientID}').value = 'Changed';");

        await result.State.btn.ClickAsync();

        Assert.Equal("Initial", result.State.textBox.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoPostBack(Browser type)
    {
        var eventCount = 0;
        await using var result = await fixture.StartAsync(type, async control =>
        {
            var textBox = new TextBox
            {
                ID = "tb",
                AutoPostBack = true
            };
            textBox.TextChanged += (_, _) =>
            {
                eventCount++;
                return Task.CompletedTask;
            };

            await control.Controls.AddAsync(textBox);
            return textBox;
        });

        var element = result.State.FindBrowserElement();
        await element.TypeAsync("Hello");
        await element.PostBackAsync();

        Assert.Equal(1, eventCount);
    }
}
