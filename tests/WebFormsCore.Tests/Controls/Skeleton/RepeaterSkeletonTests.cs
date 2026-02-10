using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Skeleton;

public class RepeaterSkeletonTests(SeleniumFixture fixture)
{
    private static readonly SeleniumFixtureOptions SkeletonOptions = new()
    {
        Configure = services => services.AddWebFormsCore(b => b.AddSkeletonSupport())
    };

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingTrueThenRendersSkeletonItems(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Repeater
                    {
                        ItemTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new Label());
                        })
                    }
                ]
            };
        }, SkeletonOptions);

        // Default SkeletonItemCount is 3, so 3 skeleton items should be rendered
        var skeletons = await result.Browser.QuerySelectorAll("[data-wfc-skeleton]").ToListAsync();
        Assert.Equal(3, skeletons.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenSkeletonItemCountChangedThenRendersCorrectNumber(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Repeater
                    {
                        SkeletonItemCount = 5,
                        ItemTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new Label());
                        })
                    }
                ]
            };
        }, SkeletonOptions);

        var skeletons = await result.Browser.QuerySelectorAll("[data-wfc-skeleton]").ToListAsync();
        Assert.Equal(5, skeletons.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenNoItemTemplateThenRendersNoSkeletonItems(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Repeater()
                ]
            };
        }, SkeletonOptions);

        // No item template means no skeleton items, only the container itself
        var skeletons = await result.Browser.QuerySelectorAll("[data-wfc-skeleton]").ToListAsync();
        Assert.Empty(skeletons);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenHeaderAndFooterTemplateThenRendersHeaderAndFooter(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Repeater
                    {
                        HeaderTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new LiteralControl { Text = "<ul>" });
                        }),
                        ItemTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new LiteralControl { Text = "<li>" });
                            c.Controls.AddWithoutPageEvents(new Label());
                            c.Controls.AddWithoutPageEvents(new LiteralControl { Text = "</li>" });
                        }),
                        FooterTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new LiteralControl { Text = "</ul>" });
                        })
                    }
                ]
            };
        }, SkeletonOptions);

        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("<ul>", html);
        Assert.Contains("</ul>", html);
        Assert.Contains("<li>", html);

        // 3 skeleton label items inside list items
        var skeletons = await result.Browser.QuerySelectorAll("[data-wfc-skeleton]").ToListAsync();
        Assert.Equal(3, skeletons.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenInsideLazyLoaderThenRendersSkeletonBeforeLoad(Browser type)
    {
        var blocker = new TaskCompletionSource();

        await using var result = await fixture.StartAsync(type, () =>
        {
            var loader = new LazyLoader
            {
                Controls =
                [
                    new Repeater
                    {
                        SkeletonItemCount = 2,
                        ItemTemplate = new InlineTemplate(c =>
                        {
                            c.Controls.AddWithoutPageEvents(new Label());
                        })
                    }
                ]
            };

            // Block the content loaded event so skeletons remain visible
            loader.ContentLoaded += async (_, _) =>
            {
                await Task.WhenAny(blocker.Task, Task.Delay(5000));
            };

            return loader;
        }, SkeletonOptions);

        // Skeleton items should be visible before lazy load completes
        var skeletons = await result.Browser.QuerySelectorAll("[data-wfc-skeleton]").ToListAsync();
        Assert.Equal(2, skeletons.Count);

        blocker.SetResult();
    }
}
