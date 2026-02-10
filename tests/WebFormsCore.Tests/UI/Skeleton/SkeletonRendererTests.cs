using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI.Skeleton;

public class SkeletonRendererTests
{
    // Each test uses a unique control type to avoid static cache pollution
    // between tests (SkeletonRenderer uses a static ConcurrentDictionary).
    private class ExactMatchControl : WebControl;
    private class HierarchyDerivedControl : Label;
    private class MostSpecificControl : Label;
    private class FallbackControl : WebControl;
    private class UnregisteredControl : WebControl;
    private class CacheTestControl : WebControl;

    private class StubRenderer<TControl> : ISkeletonRenderer<TControl>
        where TControl : Control
    {
        public ValueTask RenderSkeletonAsync(TControl control, HtmlTextWriter writer, CancellationToken token)
            => default;
    }

    [Fact]
    public void WhenExactTypeRegisteredThenReturnsRenderer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISkeletonRenderer<ExactMatchControl>, StubRenderer<ExactMatchControl>>();
        var sp = services.BuildServiceProvider();

        var renderer = sp.GetService<ISkeletonRenderer<ExactMatchControl>>();

        Assert.NotNull(renderer);
        Assert.IsType<StubRenderer<ExactMatchControl>>(renderer);
    }

    [Fact]
    public void WhenDerivedTypeThenFallbackResolverWalksHierarchy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISkeletonRenderer<Label>, StubRenderer<Label>>();
        services.AddSingleton(typeof(ISkeletonRenderer<>), typeof(SkeletonRenderer<>));
        var sp = services.BuildServiceProvider();

        // Request renderer for derived type - will use fallback that walks hierarchy
        var renderer = sp.GetService<ISkeletonRenderer<HierarchyDerivedControl>>();

        Assert.NotNull(renderer);
        Assert.IsType<SkeletonRenderer<HierarchyDerivedControl>>(renderer);
    }

    [Fact]
    public void WhenMostSpecificRegisteredThenReturnsMostSpecific()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISkeletonRenderer<WebControl>, StubRenderer<WebControl>>();
        services.AddSingleton<ISkeletonRenderer<MostSpecificControl>, StubRenderer<MostSpecificControl>>();
        services.AddSingleton(typeof(ISkeletonRenderer<>), typeof(SkeletonRenderer<>));
        var sp = services.BuildServiceProvider();

        var renderer = sp.GetService<ISkeletonRenderer<MostSpecificControl>>();

        Assert.NotNull(renderer);
        Assert.IsType<StubRenderer<MostSpecificControl>>(renderer);
    }

    [Fact]
    public void WhenOnlyBaseRegisteredThenFallbackRendererDelegatesToBase()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISkeletonRenderer<WebControl>, StubRenderer<WebControl>>();
        services.AddSingleton(typeof(ISkeletonRenderer<>), typeof(SkeletonRenderer<>));
        var sp = services.BuildServiceProvider();

        var renderer = sp.GetService<ISkeletonRenderer<FallbackControl>>();

        Assert.NotNull(renderer);
        // The fallback renderer wraps and delegates to the base type renderer
        Assert.IsType<SkeletonRenderer<FallbackControl>>(renderer);
    }

    [Fact]
    public void WhenNoRendererRegisteredThenFallbackRendererResolves()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ISkeletonRenderer<>), typeof(SkeletonRenderer<>));
        var sp = services.BuildServiceProvider();

        var renderer = sp.GetService<ISkeletonRenderer<UnregisteredControl>>();

        // Fallback is always available when open generic is registered
        Assert.NotNull(renderer);
        Assert.IsType<SkeletonRenderer<UnregisteredControl>>(renderer);
    }

    [Fact]
    public void WhenResolvedTwiceThenReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISkeletonRenderer<CacheTestControl>, StubRenderer<CacheTestControl>>();
        var sp = services.BuildServiceProvider();

        var first = sp.GetService<ISkeletonRenderer<CacheTestControl>>();
        var second = sp.GetService<ISkeletonRenderer<CacheTestControl>>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Same(first, second);
    }
}
