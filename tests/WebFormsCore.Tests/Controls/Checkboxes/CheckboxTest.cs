using WebFormsCore.Tests.Controls.Checkboxes.Pages;
using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.Checkboxes;

public class CheckboxTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task OnCheckedChanged(Browser type)
    {
        await using var result = await fixture.StartAsync<CheckboxPostbackPage>(type);

        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.button.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.button.ClickAsync();
        Assert.Equal("Unchecked", result.Control.label.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoPostback_OnCheckedChanged(Browser type)
    {
        await using var result = await fixture.StartAsync<CheckboxAutoPostbackPage>(type);

        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Unchecked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Unchecked", result.Control.label.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithText(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            ID = "cb",
            Text = "My Checkbox",
            Checked = true
        });

        var element = result.State.FindBrowserElement();
        var label = result.Browser.QuerySelector("label");

        Assert.NotNull(label);
        Assert.Equal("My Checkbox", label.Text);
        Assert.Equal(await element.GetAttributeAsync("id"), await label.GetAttributeAsync("for"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Disabled_NoPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var cb = new CheckBox
            {
                ID = "cb",
                Checked = false,
                Enabled = false
            };

            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [cb, btn] };

            container.Controls = [panel];

            return (panel, cb, btn);
        });

        // Try to check it via JS since it's disabled
        await result.Browser.ExecuteScriptAsync($"document.getElementById('{result.State.cb.ClientID}').checked = true;");

        await result.State.btn.ClickAsync();

        Assert.False(result.State.cb.Checked);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithoutID(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            Text = "No ID Checkbox"
        });

        var label = result.Browser.QuerySelector("label");
        Assert.NotNull(label);
        Assert.Equal("No ID Checkbox", label.Text);
        
        var input = result.Browser.QuerySelector("input[type='checkbox']");
        Assert.NotNull(input);
        
        var inputId = await input.GetAttributeAsync("id");
        var labelFor = await label.GetAttributeAsync("for");
        
        Assert.NotNull(inputId);
        Assert.Equal(inputId, labelFor);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackPreservesCheckedState(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var cb = new CheckBox
            {
                ID = "cb",
                Checked = true
            };

            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [cb, btn] };

            container.Controls = [panel];

            return (panel, cb, btn);
        });

        // Initially checked
        Assert.True(result.State.cb.Checked);

        // Postback should preserve state
        await result.State.btn.ClickAsync();
        Assert.True(result.State.cb.Checked);

        // Uncheck and postback
        await result.State.cb.ClickAsync();
        await result.State.btn.ClickAsync();
        Assert.False(result.State.cb.Checked);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControlReset(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            ID = "cb",
            Text = "Test Checkbox",
            Checked = true
        });

        // Clear the control
        result.State.ClearControl();

        Assert.False(result.State.Checked);
        Assert.False(result.State.AutoPostBack);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task InputAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            ID = "cb",
            InputAttributes =
            {
                ["data-test"] = "input-data",
                ["title"] = "Input Title"
            }
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("input-data", await element.GetAttributeAsync("data-test"));
        Assert.Equal("Input Title", await element.GetAttributeAsync("title"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LabelAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            ID = "cb",
            Text = "Checkbox Label",
            LabelAttributes =
            {
                ["data-label"] = "label-data",
                ["class"] = "custom-label"
            }
        });

        var label = result.Browser.QuerySelector("label");
        Assert.NotNull(label);
        Assert.Equal("label-data", await label.GetAttributeAsync("data-label"));
        Assert.Equal("custom-label", await label.GetAttributeAsync("class"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ValidationGroup(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new CheckBox
        {
            ID = "cb",
            ValidationGroup = "Group1"
        });

        Assert.Equal("Group1", result.State.ValidationGroup);
    }
}
