using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="Repeater"/> controls.
/// Renders skeleton placeholders that mimic repeater items by instantiating the
/// item template and rendering each child as a skeleton.
/// The number of skeleton items is controlled by <see cref="RepeaterBase{TItem}.SkeletonItemCount"/>.
/// </summary>
public class RepeaterSkeletonRenderer(IServiceProvider serviceProvider) : ISkeletonRenderer<Repeater>
{
    public async ValueTask RenderSkeletonAsync(Repeater control, HtmlTextWriter writer, CancellationToken token)
    {
        var itemTemplate = control.ItemTemplate;

        if (itemTemplate is null)
        {
            return;
        }

        var skeletonCount = control.SkeletonItemCount;

        // Render header template if present
        if (control.HeaderTemplate is not null)
        {
            var header = new RepeaterItem(-1, ListItemType.Header, control);
            control.HeaderTemplate.InstantiateIn(header);
            await SkeletonContainer.RenderChildSkeletonsAsync(writer, header.Controls, serviceProvider, token);
        }

        for (var i = 0; i < skeletonCount; i++)
        {
            // Render separator template between items
            if (i > 0 && control.SeparatorTemplate is not null)
            {
                var separator = new RepeaterItem(i, ListItemType.Separator, control);
                control.SeparatorTemplate.InstantiateIn(separator);
                await SkeletonContainer.RenderChildSkeletonsAsync(writer, separator.Controls, serviceProvider, token);
            }

            var itemType = (i % 2 == 0) ? ListItemType.Item : ListItemType.AlternatingItem;
            var template = itemType == ListItemType.AlternatingItem
                ? control.AlternatingItemTemplate ?? itemTemplate
                : itemTemplate;

            var item = new RepeaterItem(i, itemType, control);
            template.InstantiateIn(item);
            await SkeletonContainer.RenderChildSkeletonsAsync(writer, item.Controls, serviceProvider, token);
        }

        // Render footer template if present
        if (control.FooterTemplate is not null)
        {
            var footer = new RepeaterItem(-1, ListItemType.Footer, control);
            control.FooterTemplate.InstantiateIn(footer);
            await SkeletonContainer.RenderChildSkeletonsAsync(writer, footer.Controls, serviceProvider, token);
        }
    }
}
