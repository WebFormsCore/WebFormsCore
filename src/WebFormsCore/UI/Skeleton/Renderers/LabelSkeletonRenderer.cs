using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="Label"/> controls.
/// Renders an inline skeleton element that approximates text width.
/// </summary>
public class LabelSkeletonRenderer : ISkeletonRenderer<Label>
{
    public async ValueTask RenderSkeletonAsync(Label control, HtmlTextWriter writer, CancellationToken token)
    {
        if (!string.IsNullOrEmpty(control.Text) && !control.HasControls())
        {
            await control.RenderAsync(writer, token);
            return;
        }

        var internalControl = (IInternalWebControl)control;
        await internalControl.AddAttributesToRender(writer, token);
        writer.MergeAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton wfc-skeleton-text");
        writer.AddAttribute("data-wfc-skeleton", null);
        writer.AddAttribute("aria-hidden", "true");

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Span);
        await writer.WriteAsync("&nbsp;");
        await writer.RenderEndTagAsync();
    }
}
