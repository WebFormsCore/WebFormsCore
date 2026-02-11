using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
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

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggeredDuringLoadThenCancelsAndReloads(Browser type)
    {
        var loadCount = 0;
        var firstLoadBlocker = new TaskCompletionSource();

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            var loader = new LazyLoader
            {
                ID = "lazyCancel",
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "initial"
                    }
                ],
                OnContentLoadedAsync = async (sender, _) =>
                {
                    var currentLoad = Interlocked.Increment(ref loadCount);

                    if (currentLoad == 1)
                    {
                        // First load: block until the test releases it (simulates slow load)
                        await Task.WhenAny(firstLoadBlocker.Task, Task.Delay(10_000));
                        label.Value.Text = "first-load";
                    }
                    else
                    {
                        // Second load (retrigger): complete immediately with new data
                        label.Value.Text = "retriggered";
                    }
                }
            };

            return loader;
        }, SkeletonOptions);

        // The initial lazy-load postback is now in-flight but blocked on the server.
        // Give the request a moment to reach the server.
        await Task.Delay(300);

        // Retrigger via JS while the first load is still in progress.
        // This should abort the first request and start a new one.
        await result.Browser.ExecuteScriptAsync("wfc.retriggerLazy('lazyCancel')");

        // Release the blocker so the first request's server handler can finish
        // (the response should be discarded because the fetch was aborted).
        firstLoadBlocker.SetResult();

        // Wait for the retriggered lazy load to complete
        await WaitForLazyLoadAsync(result.Browser);

        // The label should show data from the second (retriggered) load, not the first
        Assert.Equal("retriggered", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggerCalledThenContentLoadedFiresAgain(Browser type)
    {
        var contentLoadedCount = 0;
        Ref<Label>? label = null;
        Ref<LazyLoader>? loaderRef = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();
            loaderRef = new Ref<LazyLoader>();

            var loader = new LazyLoader
            {
                Ref = loaderRef,
                Controls =
                [
                    new Label { Ref = label, Text = "count: 0" },
                    new Button
                    {
                        ID = "btnRetrigger",
                        Text = "Retrigger",
                        OnClick = (_, _) =>
                        {
                            loaderRef!.Value.Retrigger();
                        }
                    }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedCount++;
                label!.Value.Text = $"count: {contentLoadedCount}";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        // Wait for initial lazy load
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        // Click retrigger button — this triggers a postback that calls Retrigger(),
        // which resets IsLoaded. The response renders data-wfc-lazy with UniqueID again,
        // so the client auto-triggers another lazy-load postback.
        await result.Browser.QuerySelector("button")!.ClickAsync();

        // Wait for the re-triggered lazy load to complete
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);

        // Verify the lazy loader is in loaded state
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("aria-busy", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggerCalledFromOutsideFormThenContentLoadedFiresAgain(Browser type)
    {
        var contentLoadedCount = 0;
        Ref<Label>? label = null;
        Ref<LazyLoader>? loaderRef = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();
            loaderRef = new Ref<LazyLoader>();

            var loader = new LazyLoader
            {
                Ref = loaderRef,
                Controls =
                [
                    new Label { Ref = label, Text = "count: 0" }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedCount++;
                label!.Value.Text = $"count: {contentLoadedCount}";
                return Task.CompletedTask;
            };

            // Button is outside the lazy loader's scoped form
            var outerButton = new Button
            {
                Text = "Retrigger from outside",
                OnClick = (_, _) =>
                {
                    loaderRef!.Value.Retrigger();
                }
            };

            return new Panel
            {
                Controls = [loader, outerButton]
            };
        }, SkeletonOptions);

        // Wait for initial lazy load
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        // Click the button outside the lazy loader form
        await result.Browser.QuerySelector("button")!.ClickAsync();

        // Wait for the re-triggered lazy load to complete
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggerLazyJsCalledByIdThenContentReloads(Browser type)
    {
        var contentLoadedCount = 0;
        Ref<Label>? label = null;
        Ref<LazyLoader>? loaderRef = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();
            loaderRef = new Ref<LazyLoader>();

            var loader = new LazyLoader
            {
                ID = "lazyTarget",
                Ref = loaderRef,
                Controls =
                [
                    new Label { Ref = label, Text = "count: 0" }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedCount++;
                label!.Value.Text = $"count: {contentLoadedCount}";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        // Wait for initial lazy load
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        // Use the JS API to retrigger by element ID
        var clientId = loaderRef!.Value.ClientID;
        await result.Browser.ExecuteScriptAsync($"wfc.retriggerLazy('{clientId}')");

        // Wait for the re-triggered lazy load to complete
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);

        // Verify loaded state
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("aria-busy", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggerLazyJsCalledByElementThenContentReloads(Browser type)
    {
        var contentLoadedCount = 0;
        Ref<Label>? label = null;
        Ref<LazyLoader>? loaderRef = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();
            loaderRef = new Ref<LazyLoader>();

            var loader = new LazyLoader
            {
                ID = "lazyEl",
                Ref = loaderRef,
                Controls =
                [
                    new Label { Ref = label, Text = "count: 0" }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedCount++;
                label!.Value.Text = $"count: {contentLoadedCount}";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        // Wait for initial lazy load
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        // Use the JS API to retrigger by passing an element reference
        var clientId = loaderRef!.Value.ClientID;
        await result.Browser.ExecuteScriptAsync(
            $"wfc.retriggerLazy(document.getElementById('{clientId}'))");

        // Wait for the re-triggered lazy load to complete
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenRetriggerLazyJsCalledMultipleTimesThenEachFiresContentLoaded(Browser type)
    {
        var contentLoadedCount = 0;
        Ref<Label>? label = null;
        Ref<LazyLoader>? loaderRef = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();
            loaderRef = new Ref<LazyLoader>();

            var loader = new LazyLoader
            {
                ID = "lazyMulti",
                Ref = loaderRef,
                Controls =
                [
                    new Label { Ref = label, Text = "count: 0" }
                ]
            };

            loader.ContentLoaded += (_, _) =>
            {
                contentLoadedCount++;
                label!.Value.Text = $"count: {contentLoadedCount}";
                return Task.CompletedTask;
            };

            return loader;
        }, SkeletonOptions);

        // Wait for initial lazy load
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        var clientId = loaderRef!.Value.ClientID;

        // Retrigger twice sequentially
        await result.Browser.ExecuteScriptAsync($"wfc.retriggerLazy('{clientId}')");
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.ExecuteScriptAsync($"wfc.retriggerLazy('{clientId}')");
        await WaitForLazyLoadAsync(result.Browser);
        Assert.Equal("count: 3", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ScopedLazyLoadersPreservedAfterExternalPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var labelA = new Ref<Label>();
            var labelB = new Ref<Label>();

            return new Panel
            {
                Controls =
                [
                    new Button
                    {
                        Text = "External Postback",
                        CssClass = "external-btn",
                    },
                    new LazyLoader
                    {
                        ID = "loaderA",
                        AutoLoad = true,
                        Controls =
                        [
                            new Label
                            {
                                Ref = labelA,
                                Text = "A:Initial",
                                CssClass = "label-a",
                            },
                            new Button
                            {
                                Text = "Update A",
                                CssClass = "btn-a",
                                OnClick = (_, _) => labelA.Value.Text = "A:Clicked",
                            }
                        ]
                    },
                    new LazyLoader
                    {
                        ID = "loaderB",
                        AutoLoad = true,
                        Controls =
                        [
                            new Label
                            {
                                Ref = labelB,
                                Text = "B:Initial",
                                CssClass = "label-b",
                            },
                            new Button
                            {
                                Text = "Update B",
                                CssClass = "btn-b",
                                OnClick = (_, _) => labelB.Value.Text = "B:Clicked",
                            }
                        ]
                    }
                ]
            };
        });

        // Wait for both lazy loaders to auto-load
        await WaitForLazyLoadAsync(result.Browser);

        // Verify initial content
        Assert.Equal("A:Initial", result.Browser.QuerySelector(".label-a")?.Text);
        Assert.Equal("B:Initial", result.Browser.QuerySelector(".label-b")?.Text);

        // Click buttons to update labels via scoped postbacks
        await result.Browser.QuerySelector(".btn-a")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".label-a")?.Text);

        await result.Browser.QuerySelector(".btn-b")!.ClickAsync();
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".label-b")?.Text);

        // External postback — scoped content should be preserved
        await result.Browser.QuerySelector(".external-btn")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".label-a")?.Text);
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".label-b")?.Text);

        // A second external postback should also preserve
        await result.Browser.QuerySelector(".external-btn")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".label-a")?.Text);
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".label-b")?.Text);
    }
}
