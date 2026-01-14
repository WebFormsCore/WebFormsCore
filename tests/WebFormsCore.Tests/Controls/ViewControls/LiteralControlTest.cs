using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Tests.Controls.ViewControls;

public class LiteralControlTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderText(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new LiteralControl { Text = "Hello World" }]
        });

        var element = result.State.FindBrowserElement();
        Assert.Equal("Hello World", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderEmpty(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new LiteralControl { Text = "" }]
        });

        var element = result.State.FindBrowserElement();
        Assert.Equal("", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TextProperty(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new LiteralControl());

        // Test getting/setting text
        Assert.Equal(string.Empty, result.State.Text);
        
        result.State.Text = "New Text";
        Assert.Equal("New Text", result.State.Text);

        result.State.Text = null!;
        Assert.Equal(string.Empty, result.State.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task NoChildControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new LiteralControl
        {
            Text = "Test"
        });

        // LiteralControl should have EmptyControlCollection
        Assert.False(result.State.HasControls());
        Assert.Empty(result.State.Controls);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task EnableViewStateThrows(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new LiteralControl());

        // EnableViewState should always be false for LiteralControl
        Assert.False(result.State.EnableViewState);

        // WebFormsCore-specific: Setting EnableViewState to true throws InvalidOperationException.
        // In .NET FX WebForms, setting it doesn't throw but ViewState is still not persisted.
        // WebFormsCore enforces this more strictly to prevent developer confusion.
        Assert.Throws<InvalidOperationException>(() => result.State.EnableViewState = true);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControlReset(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new LiteralControl
        {
            Text = "Initial Text"
        });

        result.State.ClearControl();

        Assert.Equal(string.Empty, result.State.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderHtmlContent(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new LiteralControl { Text = "<b>Bold</b> text" }]
        });

        var element = result.State.FindBrowserElement();
        var text = element.Text;
        // LiteralControl writes HTML directly and browser parses it
        Assert.Equal("Bold text", text);
    }
}
