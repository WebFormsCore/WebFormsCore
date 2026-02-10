using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Skeleton;

public class LazyLoaderTests(SeleniumFixture fixture)
{
    private static readonly SeleniumFixtureOptions SkeletonOptions = new()
    {
        Configure = services => services.AddWebFormsCore(b => b.AddSkeletonSupport())
    };

    /// <summary>
    /// Waits for the lazy loader's auto-postback to complete by polling for the
    /// data-wfc-lazy attribute to become empty (loaded state).
    /// </summary>
    private static async Task WaitForLazyLoadAsync(ITestContext browser, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // When loaded, data-wfc-lazy has an empty value
                if (browser.QuerySelector("[data-wfc-lazy]:not([data-wfc-lazy=''])") is null)
                    return;
            }
            catch
            {
                // Page may be reloading during navigation
            }

            await Task.Delay(100);
        }
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenAutoPostbackCompletesThenRendersContent(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new LazyLoader
            {
                Controls =
                [
                    new Label { Text = "Lazy content" }
                ]
            };
        }, SkeletonOptions);

        await WaitForLazyLoadAsync(result.Browser);

        Assert.Equal("Lazy content", result.Browser.QuerySelector("span")?.Text);
        // When loaded, data-wfc-lazy is present but empty
        Assert.Null(result.Browser.QuerySelector("[data-wfc-lazy]:not([data-wfc-lazy=''])"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenAutoPostbackCompletesThenRemovesAriaBusy(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new LazyLoader
            {
                Controls =
                [
                    new Label { Text = "Content" }
                ]
            };
        }, SkeletonOptions);

        await WaitForLazyLoadAsync(result.Browser);

        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("aria-busy", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenContentLoadedSubscribedThenEventFires(Browser type)
    {
        Ref<Label>? label = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();

            var loader = new LazyLoader
            {
                Controls =
                [
                    new Label { Ref = label, Text = "Before" }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                label!.Value.Text = "Loaded!";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        await WaitForLazyLoadAsync(result.Browser);

        Assert.Equal("Loaded!", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenNotVisibleThenRendersNothing(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new LazyLoader
            {
                Visible = false,
                Controls =
                [
                    new Label { Text = "Hidden content" }
                ]
            };
        }, SkeletonOptions);

        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Hidden content", html);
        Assert.Null(result.Browser.QuerySelector("[data-wfc-lazy]"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenChildButtonClickedAfterLoadThenContentLoadedDoesNotFireAgain(Browser type)
    {
        var loadCount = 0;
        var contentLoadedFired = 0;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            var loader = new LazyLoader
            {
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "count: 0"
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) =>
                        {
                            loadCount++;
                            label!.Value.Text = $"count: {loadCount}";
                        }
                    }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedFired++;
                loadCount++;
                label!.Value.Text = $"count: {loadCount}";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        // Wait for initial auto-load
        await WaitForLazyLoadAsync(result.Browser);

        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("count: 3", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyLoadPostbackInbetween(Browser type)
    {
        var requestBlocker = new TaskCompletionSource();

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();
            var button = new Ref<Button>();

            var loader = new LazyLoader
            {
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = ""
                    }
                ],
                OnContentLoadedAsync = async (sender, _) =>
                {
                    await Task.WhenAny(requestBlocker.Task, Task.Delay(1000));
                    label.Value.Text = "Loaded";
                }
            };

            var panel = new Panel
            {
                Controls =
                [
                    new Button
                    {
                        Ref = button,
                        Text = "Trigger postback",
                        OnClick = (_, _) =>
                        {
                            button.Value.Text = "Clicked";
                        }
                    }
                ]
            };

            return new Panel
            {
                Controls = [loader, panel]
            };
        }, SkeletonOptions);

        Assert.NotEqual("Loaded", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Clicked", result.Browser.QuerySelector("button")?.Text);

        requestBlocker.SetResult();

        Assert.Equal("Loaded", result.Browser.QuerySelector("span")?.Text);
        Assert.Equal("Clicked", result.Browser.QuerySelector("button")?.Text);
    }
}
