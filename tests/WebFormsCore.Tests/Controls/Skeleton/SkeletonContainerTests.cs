using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Skeleton;

public class SkeletonContainerTests(SeleniumFixture fixture)
{
    private static readonly SeleniumFixtureOptions SkeletonOptions = new()
    {
        Configure = services => services.AddWebFormsCore(b => b.AddSkeletonSupport())
    };

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingFalseThenRendersChildrenNormally(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = false,
                Controls =
                [
                    new Label { Text = "Hello" }
                ]
            };
        }, SkeletonOptions);

        Assert.Equal("Hello", result.Browser.QuerySelector("span")?.Text);
        Assert.Null(result.Browser.QuerySelector("[data-wfc-skeleton]"));
        Assert.Null(result.Browser.QuerySelector("[data-wfc-skeleton-container]"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingTrueThenRendersSkeletonPlaceholders(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Label { Text = "Hello" }
                ]
            };
        }, SkeletonOptions);

        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Hello", html);
        Assert.NotNull(result.Browser.QuerySelector("[data-wfc-skeleton]"));
        Assert.NotNull(result.Browser.QuerySelector(".wfc-skeleton"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingTrueThenAddsContainerAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Label { Text = "Test" }
                ]
            };
        }, SkeletonOptions);

        Assert.NotNull(result.Browser.QuerySelector("[data-wfc-skeleton-container]"));
        Assert.NotNull(result.Browser.QuerySelector("[aria-busy=\"true\"]"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingCssClassSetThenAddsClass(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                LoadingCssClass = "is-loading",
                Controls =
                [
                    new Label { Text = "Test" }
                ]
            };
        }, SkeletonOptions);

        Assert.NotNull(result.Browser.QuerySelector(".is-loading"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenButtonChildThenRendersDisabledButtonSkeleton(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Button { Text = "Submit" }
                ]
            };
        }, SkeletonOptions);

        // Check that the button skeleton is rendered without the actual button text
        var button = result.Browser.QuerySelector(".wfc-skeleton-button[disabled]");
        Assert.NotNull(button);
        Assert.DoesNotContain("Submit", button?.Text ?? "");
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenTextBoxChildThenRendersDisabledInputSkeleton(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new TextBox()
                ]
            };
        }, SkeletonOptions);

        Assert.NotNull(result.Browser.QuerySelector(".wfc-skeleton-input[disabled]"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenChildNotVisibleThenSkipsIt(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Label { Text = "Hidden", Visible = false }
                ]
            };
        }, SkeletonOptions);

        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Hidden", html);
        Assert.Null(result.Browser.QuerySelector("[data-wfc-skeleton]"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLiteralControlChildThenRendersAsIs(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new LiteralControl { Text = "<hr />" }
                ]
            };
        }, SkeletonOptions);

        Assert.NotNull(result.Browser.QuerySelector("hr"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenToggleLoadingThenContentAppears(Browser type)
    {
        SkeletonContainer? container = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            container = new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Label { Text = "Real content" }
                ]
            };

            return new Panel
            {
                Controls =
                [
                    container,
                    new Button
                    {
                        Text = "Toggle",
                        OnClick = (_, _) => container!.Loading = false,
                    }
                ]
            };
        }, SkeletonOptions);

        // Initially loading - real content should not be visible
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Real content", html);
        Assert.NotNull(result.Browser.QuerySelector("[data-wfc-skeleton]"));

        // Click toggle button to set Loading = false
        await result.Browser.QuerySelector("button")!.ClickAsync();

        // Real content should now be visible
        Assert.Equal("Real content", result.Browser.QuerySelector("span")?.Text);
        Assert.Null(result.Browser.QuerySelector("[data-wfc-skeleton]"));
    }
}
