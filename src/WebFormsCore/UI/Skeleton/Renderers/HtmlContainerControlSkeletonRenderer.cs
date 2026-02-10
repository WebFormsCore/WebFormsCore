using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="HtmlContainerControl"/>.
/// Preserves the tag name and attributes, replaces content with a skeleton placeholder.
/// </summary>
public class HtmlContainerControlSkeletonRenderer : ISkeletonRenderer<HtmlContainerControl>
{
    public async ValueTask RenderSkeletonAsync(HtmlContainerControl control, HtmlTextWriter writer, CancellationToken token)
    {
        await writer.WriteBeginTagAsync(control.TagName);
        await control.RenderAttributesInternalAsync(writer);

        var existingClass = control.Attributes["class"];
        var skeletonClass = string.IsNullOrEmpty(existingClass)
            ? "wfc-skeleton"
            : existingClass + " wfc-skeleton";

        await writer.WriteAttributeAsync("class", skeletonClass);
        await writer.WriteAttributeAsync("data-wfc-skeleton", null);
        await writer.WriteAttributeAsync("aria-hidden", "true");
        await writer.WriteAsync('>');

        await writer.WriteAsync("&nbsp;");

        await writer.WriteEndTagAsync(control.TagName);
    }
}
