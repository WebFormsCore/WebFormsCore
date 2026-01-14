using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Tests.Controls.Text;

public class LiteralTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderEncode(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new Literal { ID = "literal", Text = "<b>Bold</b>", Mode = LiteralMode.Encode }]
        });

        var element = result.State.FindBrowserElement();
        var html = await element.GetAttributeAsync("innerHTML");
        Assert.Contains("&lt;b&gt;Bold&lt;/b&gt;", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderPassThrough(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new Literal { ID = "literal", Text = "<b>Bold</b>", Mode = LiteralMode.PassThrough }]
        });

        var element = result.State.FindBrowserElement();
        var html = await element.GetAttributeAsync("innerHTML");
        Assert.Contains("<b>Bold</b>", html);
        Assert.Equal("Bold", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderTransform(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new Literal { ID = "literal", Text = "<b>Bold</b>", Mode = LiteralMode.Transform }]
        });

        var element = result.State.FindBrowserElement();
        var html = await element.GetAttributeAsync("innerHTML");
        Assert.Contains("<b>Bold</b>", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderEmpty(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new Literal { ID = "literal", Text = string.Empty, Mode = LiteralMode.Encode }]
        });

        var element = result.State.FindBrowserElement();
        var html = await element.GetAttributeAsync("innerHTML");
        Assert.Equal("", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderNull(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HtmlGenericControl("div")
        {
            ID = "container",
            Controls = [new Literal { ID = "literal", Text = null, Mode = LiteralMode.Encode }]
        });

        var element = result.State.FindBrowserElement();
        var html = await element.GetAttributeAsync("innerHTML");
        Assert.Equal("", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControlReset(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Literal
        {
            ID = "literal",
            Text = "Initial Text",
            Mode = LiteralMode.Encode
        });

        // Clear the control
        result.State.ClearControl();

        Assert.Null(result.State.Text);
        Assert.Equal(LiteralMode.Transform, result.State.Mode);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TextPropertyInterface(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Literal
        {
            ID = "literal",
            Text = null
        });

        // Test ITextControl interface
        ITextControl textControl = result.State;
        Assert.Equal(string.Empty, textControl.Text);

        textControl.Text = "New Text";
        Assert.Equal("New Text", result.State.Text);
    }
}
