using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Generic fallback skeleton renderer for any <see cref="WebControl"/>.
/// Renders the same tag type with preserved id and class attributes,
/// replacing content with a skeleton placeholder.
/// </summary>
public class WebControlSkeletonRenderer : ISkeletonRenderer<WebControl>
{
    public async ValueTask RenderSkeletonAsync(WebControl control, HtmlTextWriter writer, CancellationToken token)
    {
        var internalControl = (IInternalWebControl)control;
        await internalControl.AddAttributesToRender(writer, token);
        writer.MergeAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton");
        writer.AddAttribute("data-wfc-skeleton", null);
        writer.AddAttribute("aria-hidden", "true");

        await writer.RenderBeginTagAsync(internalControl.TagName);
        await writer.WriteAsync("&nbsp;");
        await writer.RenderEndTagAsync();
    }
}
