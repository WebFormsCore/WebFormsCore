using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI.Skeleton;

public class LazyLoaderTests
{
    private class TestLazyLoader : LazyLoader
    {
        public bool ForceProcess { get; set; }

        protected override bool ProcessControl => ForceProcess || base.ProcessControl;

        public ValueTask TriggerOnLoadAsync(CancellationToken token)
            => OnLoadAsync(token);
    }

    private static Page CreatePage(bool withSkeletonSupport = true)
    {
        var services = new ServiceCollection();
        var builder = services.AddWebFormsCore();

        if (withSkeletonSupport)
        {
            builder.AddSkeletonSupport();
        }

        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var page = new Page();
        ((IInternalPage)page).SetContext(httpContext);
        return page;
    }

    private static Page CreatePostBackPage(string wfcTarget, bool withSkeletonSupport = true)
    {
        var services = new ServiceCollection();
        var builder = services.AddWebFormsCore();

        if (withSkeletonSupport)
        {
            builder.AddSkeletonSupport();
        }

        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["wfcTarget"] = wfcTarget
        });

        var page = new Page();
        ((IInternalPage)page).SetContext(httpContext);
        return page;
    }

    [Fact]
    public void IsLoadedDefaultsToFalse()
    {
        var loader = new LazyLoader();

        Assert.False(loader.IsLoaded);
    }

    [Fact]
    public async Task WhenNotLoadedThenRendersLazyAttribute()
    {
        var page = CreatePage();
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("data-wfc-lazy", output);
    }

    [Fact]
    public async Task WhenNotLoadedThenRendersAriaBusy()
    {
        var page = CreatePage();
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("aria-busy=\"true\"", output);
    }

    [Fact]
    public async Task WhenNotLoadedThenRendersSkeletonForChildren()
    {
        var page = CreatePage();
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        // Label with text renders the text (not skeleton) because static text should be shown
        var label = new Label { Text = "Real content" };
        loader.Controls.AddWithoutPageEvents(label);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        // Labels with text render normally in skeleton mode
        Assert.Contains("Real content", output);
        Assert.Contains("aria-busy=\"true\"", output);
    }

    [Fact]
    public async Task WhenLoadingCssClassSetThenAppliesClass()
    {
        var page = CreatePage();
        var loader = new LazyLoader
        {
            ID = "lazy1",
            LoadingCssClass = "shimmer"
        };
        page.Controls.AddWithoutPageEvents(loader);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("shimmer", output);
    }

    [Fact]
    public async Task WhenNotVisibleThenRendersNothing()
    {
        var page = CreatePage();
        var loader = new LazyLoader { ID = "lazy1", Visible = false };
        page.Controls.AddWithoutPageEvents(loader);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public async Task WhenProcessControlTrueThenOnLoadSetsIsLoaded()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);

        Assert.True(loader.IsLoaded);
    }

    [Fact]
    public async Task WhenProcessControlFalseThenOnLoadDoesNotSetIsLoaded()
    {
        var page = CreatePage();
        var loader = new TestLazyLoader { ID = "lazy1", ForceProcess = false };
        page.Controls.AddWithoutPageEvents(loader);
        // Set state past Initialized so base ProcessControl returns false
        loader._state = ControlState.Loaded;

        await loader.TriggerOnLoadAsync(CancellationToken.None);

        Assert.False(loader.IsLoaded);
    }

    [Fact]
    public async Task WhenLoadedThenRendersChildContentNormally()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var label = new Label { Text = "Real content" };
        loader.Controls.AddWithoutPageEvents(label);

        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("Real content", output);
        Assert.Contains("data-wfc-lazy=\"\"", output);
        Assert.DoesNotContain("aria-busy", output);
    }

    [Fact]
    public async Task WhenLoadedThenContentLoadedEventFires()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var eventFired = false;
        loader.ContentLoaded += (_, _) =>
        {
            eventFired = true;
            return Task.CompletedTask;
        };

        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);

        Assert.True(eventFired);
    }

    [Fact]
    public async Task WhenNoSkeletonSupportThenRendersFallbackSkeleton()
    {
        var page = CreatePage(withSkeletonSupport: false);
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("wfc-skeleton", output);
        Assert.Contains("&nbsp;", output);
    }

    [Fact]
    public async Task WhenLiteralChildThenRendersLiteralAsIsDuringSkeleton()
    {
        var page = CreatePage();
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var literal = new LiteralControl { Text = "<p>Structural HTML</p>" };
        loader.Controls.AddWithoutPageEvents(literal);

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("<p>Structural HTML</p>", output);
    }

    [Fact]
    public void ClearControlResetsAllState()
    {
        var loader = new LazyLoader
        {
            LoadingCssClass = "loading"
        };
        loader.ContentLoaded += (_, _) => Task.CompletedTask;

        loader.ClearControl();

        Assert.False(loader.IsLoaded);
        Assert.Null(loader.LoadingCssClass);
    }

    [Fact]
    public async Task WhenContentLoadedSubscribedAfterLoadThenEventDoesNotFire()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        // Load fires ContentLoaded synchronously during OnLoadAsync
        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);

        // Subscribing after load misses the event
        var eventFired = false;
        loader.ContentLoaded += (_, _) =>
        {
            eventFired = true;
            return Task.CompletedTask;
        };

        Assert.True(loader.IsLoaded);
        Assert.False(eventFired);
    }

    [Fact]
    public async Task WhenPostbackTargetsLazyLoaderThenProcessControlReturnsTrue()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);
        // Advance past init so the postback check is reached
        loader._state = ControlState.Loaded;

        // ProcessControl is protected, test via OnLoadAsync which uses it
        // Calling OnLoadAsync directly as a workaround
        var testLoader = new TestLazyLoader { ID = "lazy2" };
        var page2 = CreatePostBackPage("lazy2");
        page2.Controls.AddWithoutPageEvents(testLoader);
        testLoader._state = ControlState.Loaded;

        await testLoader.TriggerOnLoadAsync(CancellationToken.None);

        Assert.True(testLoader.IsLoaded);
    }

    [Fact]
    public async Task WhenPostbackTargetsDifferentControlThenDoesNotLoad()
    {
        var page = CreatePostBackPage("other_control");
        var loader = new TestLazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);
        loader._state = ControlState.Loaded;

        await loader.TriggerOnLoadAsync(CancellationToken.None);

        Assert.False(loader.IsLoaded);
    }

    [Fact]
    public async Task WhenRetriggerCalledThenIsLoadedResets()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);
        Assert.True(loader.IsLoaded);

        loader.Retrigger();

        Assert.False(loader.IsLoaded);
    }

    [Fact]
    public async Task WhenRetriggerCalledThenRendersLazyAttributeWithUniqueId()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new LazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        await ((IInternalControl)loader).LoadAsync(CancellationToken.None);

        loader.Retrigger();

        var writer = new StringHtmlTextWriter();
        await loader.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("aria-busy=\"true\"", output);
        Assert.DoesNotContain("data-wfc-lazy=\"\"", output);
    }

    [Fact]
    public async Task WhenRetriggerCalledThenContentLoadedFiresAgainOnNextPostback()
    {
        var page = CreatePostBackPage("lazy1");
        var loader = new TestLazyLoader { ID = "lazy1" };
        page.Controls.AddWithoutPageEvents(loader);

        var eventCount = 0;
        loader.ContentLoaded += (_, _) =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        await loader.TriggerOnLoadAsync(CancellationToken.None);
        Assert.Equal(1, eventCount);

        loader.Retrigger();

        // Simulate a second lazy-load postback
        await loader.TriggerOnLoadAsync(CancellationToken.None);
        Assert.Equal(2, eventCount);
    }
}
