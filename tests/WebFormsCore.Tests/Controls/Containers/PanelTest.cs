using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.Containers;

public class PanelTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderBasic(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel"
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("div", (await element.GetAttributeAsync("tagName"))?.ToLower());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithContent(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Literal { Text = "Panel Content" }
            ]
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("Panel Content", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithMultipleControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Label { Text = "Label" },
                new Literal { Text = " " },
                new TextBox { ID = "tb", Text = "TextBox" }
            ]
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Contains("Label", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithCssClass(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            CssClass = "my-panel",
            Controls = [new Literal { Text = "Content" }]
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("my-panel", await element.GetAttributeAsync("class"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Attributes = { ["data-test"] = "panel-data" },
            Controls = [new Literal { Text = "Panel with Attributes" }]
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("panel-data", await element.GetAttributeAsync("data-test"));
    }
}
