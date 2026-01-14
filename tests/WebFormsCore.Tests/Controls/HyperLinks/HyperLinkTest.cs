using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.HyperLinks;

public class HyperLinkTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderBasic(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            Text = "Click me",
            NavigateUrl = "https://example.com",
            Target = "_blank"
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("https://example.com/", await element.GetAttributeAsync("href"));
        Assert.Equal("_blank", await element.GetAttributeAsync("target"));
        Assert.Equal("Click me", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            NavigateUrl = "https://example.com",
            Controls = [new Literal { Text = "<b>Bold text</b>", Mode = LiteralMode.PassThrough }]
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("https://example.com/", await element.GetAttributeAsync("href"));
        Assert.Equal("Bold text", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithoutNavigateUrl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            Text = "No link"
        });

        var element = result.State.FindBrowserElement();

        Assert.Null(await element.GetAttributeAsync("href"));
        Assert.Equal("No link", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithoutTarget(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            Text = "Link",
            NavigateUrl = "https://example.com"
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("https://example.com/", await element.GetAttributeAsync("href"));
        var target = await element.GetAttributeAsync("target");
        Assert.True(string.IsNullOrEmpty(target));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderEmptyNavigateUrl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            Text = "Empty URL",
            NavigateUrl = string.Empty
        });

        var element = result.State.FindBrowserElement();

        Assert.Null(await element.GetAttributeAsync("href"));
        Assert.Equal("Empty URL", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderEmptyTarget(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new HyperLink
        {
            ID = "link",
            Text = "Link",
            NavigateUrl = "https://example.com",
            Target = string.Empty
        });

        var element = result.State.FindBrowserElement();

        Assert.Equal("https://example.com/", await element.GetAttributeAsync("href"));
        var target = await element.GetAttributeAsync("target");
        Assert.True(string.IsNullOrEmpty(target));
    }
}
