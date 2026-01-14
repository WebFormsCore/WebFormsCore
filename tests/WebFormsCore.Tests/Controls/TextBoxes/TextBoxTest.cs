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
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.Password
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("password", await textBoxElement.GetAttributeAsync("type"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ReadOnlyTextBox(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var textBox = new TextBox
            {
                ID = "textBox",
                Text = "ReadOnly",
                ReadOnly = true
            };
            var btn = new Button { ID = "button" };
            var panel = new Panel { Controls = [textBox, btn] };

            container.Controls = [panel];

            return (panel, textBox, btn);
        });

        var textBoxElement = result.State.textBox.FindBrowserElement();

        Assert.Equal("true", await textBoxElement.GetAttributeAsync("readonly"));
        Assert.Equal("ReadOnly", textBoxElement.Value);

        // Try to change via JavaScript (simulating user interaction)
        await result.Browser.ExecuteScriptAsync($"document.getElementById('{result.State.textBox.ClientID}').value = 'Changed';");
        
        // Postback - readonly should preserve original value
        await result.State.btn.ClickAsync();
        Assert.Equal("ReadOnly", result.State.textBox.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task MaxLengthAttribute(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            MaxLength = 10
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("10", await textBoxElement.GetAttributeAsync("maxlength"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task MultiLineTextBox(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var textBox = new TextBox
            {
                ID = "textBox",
                TextMode = TextBoxMode.MultiLine,
                Rows = 5,
                Columns = 40,
                Text = "Multi\nLine\nText"
            };

            var btn = new Button { ID = "button" };
            var panel = new Panel { Controls = [textBox, btn] };

            container.Controls = [panel];

            return (panel, textBox, btn);
        });

        var textBoxElement = result.State.textBox.FindBrowserElement();

        Assert.Equal("textarea", (await textBoxElement.GetAttributeAsync("tagName"))?.ToLower());
        Assert.Equal("5", await textBoxElement.GetAttributeAsync("rows"));
        Assert.Equal("40", await textBoxElement.GetAttributeAsync("cols"));
        Assert.Contains("Multi", textBoxElement.Value);

        // Change and postback
        await textBoxElement.ClearAsync();
        await textBoxElement.TypeAsync("New Text");
        await result.State.btn.ClickAsync();
        Assert.Equal("New Text", result.State.textBox.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task MultiLineNoWrap(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.MultiLine,
            Wrap = false
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("off", await textBoxElement.GetAttributeAsync("wrap"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoCompleteDisabled(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            AutoCompleteType = AutoCompleteType.Disabled
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("off", await textBoxElement.GetAttributeAsync("autocomplete"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoCompleteEnabled(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            AutoCompleteType = AutoCompleteType.Enabled
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("on", await textBoxElement.GetAttributeAsync("autocomplete"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task EmailType(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.Email
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("email", await textBoxElement.GetAttributeAsync("type"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task NumberType(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.Number
        });

        var textBoxElement = result.State.FindBrowserElement();

        Assert.Equal("number", await textBoxElement.GetAttributeAsync("type"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            Text = "Initial",
            ReadOnly = true
        });

        result.State.ClearControl();

        // ClearControl only resets base WebControl properties (Enabled, ToolTip, Attributes)
        // It doesn't reset TextBox-specific properties like ReadOnly or TextMode
        Assert.True(result.State.Enabled);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ValidationGroup(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            ValidationGroup = "Group1"
        });

        Assert.Equal("Group1", result.State.ValidationGroup);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task CausesValidation(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            CausesValidation = false
        });

        Assert.False(result.State.CausesValidation);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ColumnsAndRows(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.MultiLine,
            Columns = 40,
            Rows = 10
        });

        var textarea = result.Browser.QuerySelector("textarea#textBox");
        Assert.NotNull(textarea);
        Assert.Equal("40", await textarea.GetAttributeAsync("cols"));
        Assert.Equal("10", await textarea.GetAttributeAsync("rows"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TextChangedEvent(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, async control =>
        {
            var textBox = new TextBox
            {
                ID = "textBox",
                AutoPostBack = true
            };

            textBox.TextChanged += (sender, args) =>
            {
                eventFired = true;
                return Task.CompletedTask;
            };

            await control.Controls.AddAsync(textBox);

            return textBox;
        });

        var element = result.State.FindBrowserElement();
        await element.TypeAsync("New Text");
        await element.PostBackAsync();

        Assert.True(eventFired);
        Assert.Equal("New Text", result.State.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PasswordTextMode(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            ID = "textBox",
            TextMode = TextBoxMode.Password,
            Text = "Secret"
        });

        var input = result.Browser.QuerySelector("input#textBox");
        Assert.NotNull(input);
        Assert.Equal("password", await input.GetAttributeAsync("type"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TextBoxWithNoId(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TextBox
        {
            Text = "Test"
        });

        var input = result.Browser.QuerySelector("input[type='text']");
        Assert.NotNull(input);
    }}