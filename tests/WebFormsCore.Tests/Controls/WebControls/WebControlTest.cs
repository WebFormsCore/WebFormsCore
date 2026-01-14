using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.WebControls;

public class WebControlTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            CssClass = "my-class",
            ToolTip = "Some help text",
            TabIndex = 5
        });

        var element = result.State.FindBrowserElement();

        Assert.Contains("my-class", await element.GetAttributeAsync("class"));
        Assert.Equal("Some help text", await element.GetAttributeAsync("title"));
        Assert.Equal("5", await element.GetAttributeAsync("tabindex"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderDisabled(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Enabled = false
        });

        var element = result.State.FindBrowserElement();

        // Panel is a Div, so it shouldn't have 'disabled' attribute, but aria-disabled and data-wfc-disabled
        Assert.Equal("true", await element.GetAttributeAsync("aria-disabled"));
        Assert.Equal("true", await element.GetAttributeAsync("data-wfc-disabled"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RecursiveEnabled(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "parent",
            Enabled = false,
            Controls =
            [
                new Panel { ID = "child", Enabled = true }
            ]
        });

        var element = result.Browser.QuerySelector("#child")!;

        // Even though child.Enabled is true, it should render as disabled because parent is disabled
        Assert.Equal("true", await element.GetAttributeAsync("aria-disabled"));
    }
}
