using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="Grid"/> controls.
/// Renders a table with skeleton placeholders that mimic the grid's column structure.
/// The number of skeleton rows is controlled by <see cref="RepeaterBase{TItem}.SkeletonItemCount"/>.
/// </summary>
public class GridSkeletonRenderer : ISkeletonRenderer<Grid>
{
    public async ValueTask RenderSkeletonAsync(Grid control, HtmlTextWriter writer, CancellationToken token)
    {
        var columns = control.Columns;

        if (columns.Count == 0)
        {
            return;
        }

        var skeletonCount = control.SkeletonItemCount;

        // Render CSS class if set on the Grid
        if (!string.IsNullOrEmpty(control.CssClass))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, control.CssClass);
        }

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Table);

        // Render <thead> with column headers
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Thead);
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

        foreach (var column in columns)
        {
            if (!column.Visible) continue;

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Th);
            await writer.WriteAsync(column.HeaderText);
            await writer.RenderEndTagAsync(); // </th>
        }

        await writer.RenderEndTagAsync(); // </tr>
        await writer.RenderEndTagAsync(); // </thead>

        // Render <tbody> with skeleton rows
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tbody);

        // Count visible columns for the colspan
        var visibleColumnCount = 0;

        foreach (var column in columns)
        {
            if (column.Visible) visibleColumnCount++;
        }

        for (var i = 0; i < skeletonCount; i++)
        {
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

            // Render a single cell spanning the entire row for a full-row skeleton effect
            writer.AddAttribute(HtmlTextWriterAttribute.Colspan, visibleColumnCount.ToString());
            writer.MergeAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton wfc-skeleton-text");
            writer.AddAttribute("data-wfc-skeleton", null);
            writer.AddAttribute("aria-hidden", "true");

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Td);
            await writer.WriteAsync("&nbsp;");
            await writer.RenderEndTagAsync(); // </td>

            await writer.RenderEndTagAsync(); // </tr>
        }

        await writer.RenderEndTagAsync(); // </tbody>
        await writer.RenderEndTagAsync(); // </table>
    }
}
