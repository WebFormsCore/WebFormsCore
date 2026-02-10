using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Skeleton;

public class GridSkeletonTests(SeleniumFixture fixture)
{
    private static readonly SeleniumFixtureOptions SkeletonOptions = new()
    {
        Configure = services => services.AddWebFormsCore(b =>
        {
            b.AddSkeletonSupport();
            b.AddGridSkeletonSupport();
        })
    };

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenLoadingTrueThenRendersSkeletonTable(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Grid
                    {
                        Columns =
                        {
                            new GridBoundColumn { HeaderText = "Name" },
                            new GridBoundColumn { HeaderText = "Age" }
                        }
                    }
                ]
            };
        }, SkeletonOptions);

        // Should render a table
        Assert.NotNull(result.Browser.QuerySelector("table"));

        // Should render thead with column headers
        Assert.NotNull(result.Browser.QuerySelector("thead"));
        var headers = await result.Browser.QuerySelectorAll("th").ToListAsync();
        Assert.Equal(2, headers.Count);

        // Should render 3 skeleton rows (default SkeletonItemCount)
        var rows = await result.Browser.QuerySelectorAll("tbody tr").ToListAsync();
        Assert.Equal(3, rows.Count);

        // Each row should have a single skeleton cell with colspan
        var skeletonCells = await result.Browser.QuerySelectorAll("tbody td[data-wfc-skeleton]").ToListAsync();
        Assert.Equal(3, skeletonCells.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenSkeletonItemCountChangedThenRendersCorrectRows(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Grid
                    {
                        SkeletonItemCount = 5,
                        Columns =
                        {
                            new GridBoundColumn { HeaderText = "Col1" }
                        }
                    }
                ]
            };
        }, SkeletonOptions);

        var rows = await result.Browser.QuerySelectorAll("tbody tr").ToListAsync();
        Assert.Equal(5, rows.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenNoColumnsThenRendersNothing(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Grid()
                ]
            };
        }, SkeletonOptions);

        Assert.Null(result.Browser.QuerySelector("table"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenCssClassSetThenAppliedToTable(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new SkeletonContainer
            {
                Loading = true,
                Controls =
                [
                    new Grid
                    {
                        CssClass = "my-grid",
                        Columns =
                        {
                            new GridBoundColumn { HeaderText = "Test" }
                        }
                    }
                ]
            };
        }, SkeletonOptions);

        Assert.NotNull(result.Browser.QuerySelector("table.my-grid"));
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
                    new Grid
                    {
                        SkeletonItemCount = 2,
                        Columns =
                        {
                            new GridBoundColumn { HeaderText = "Name" },
                            new GridBoundColumn { HeaderText = "Value" }
                        }
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

        // Skeleton table should be visible before lazy load completes
        Assert.NotNull(result.Browser.QuerySelector("table"));
        var rows = await result.Browser.QuerySelectorAll("tbody tr").ToListAsync();
        Assert.Equal(2, rows.Count);

        blocker.SetResult();
    }
}
