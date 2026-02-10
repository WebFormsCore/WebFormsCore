using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton.Renderers;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.UnitTests.Controls.Skeleton;

public class GridSkeletonRendererTests
{
    [Fact]
    public async Task WhenRenderingThenEachRowHasSingleCellWithColspan()
    {
        var grid = new Grid
        {
            SkeletonItemCount = 3,
            Columns =
            {
                new GridBoundColumn { HeaderText = "Name" },
                new GridBoundColumn { HeaderText = "Age" }
            }
        };

        var renderer = new GridSkeletonRenderer();
        var writer = new StringHtmlTextWriter();
        await renderer.RenderSkeletonAsync(grid, writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();

        // Should have a colspan spanning all visible columns
        Assert.Contains("colspan=\"2\"", output);

        // Should have skeleton class on the single td per row
        Assert.Contains("wfc-skeleton", output);

        // Should have 3 skeleton rows
        var rowCount = CountOccurrences(output, "<tr>");
        // 1 header row + 3 skeleton rows = 4 total
        Assert.Equal(4, rowCount);
    }

    [Fact]
    public async Task WhenColumnsHiddenThenColspanReflectsVisibleOnly()
    {
        var grid = new Grid
        {
            SkeletonItemCount = 2,
            Columns =
            {
                new GridBoundColumn { HeaderText = "Name", Visible = true },
                new GridBoundColumn { HeaderText = "Hidden", Visible = false },
                new GridBoundColumn { HeaderText = "Age", Visible = true }
            }
        };

        var renderer = new GridSkeletonRenderer();
        var writer = new StringHtmlTextWriter();
        await renderer.RenderSkeletonAsync(grid, writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();

        // Should have colspan=2 (only visible columns)
        Assert.Contains("colspan=\"2\"", output);
        Assert.DoesNotContain("colspan=\"3\"", output);
    }

    [Fact]
    public async Task WhenCssClassSetThenAppliedToTable()
    {
        var grid = new Grid
        {
            CssClass = "my-grid-class",
            SkeletonItemCount = 1,
            Columns =
            {
                new GridBoundColumn { HeaderText = "Col1" }
            }
        };

        var renderer = new GridSkeletonRenderer();
        var writer = new StringHtmlTextWriter();
        await renderer.RenderSkeletonAsync(grid, writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("my-grid-class", output);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
