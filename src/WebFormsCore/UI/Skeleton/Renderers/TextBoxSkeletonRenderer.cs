using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="TextBox"/> controls.
/// Renders an input-shaped skeleton placeholder.
/// </summary>
public class TextBoxSkeletonRenderer : ISkeletonRenderer<TextBox>
{
    public async ValueTask RenderSkeletonAsync(TextBox control, HtmlTextWriter writer, CancellationToken token)
    {
        var internalControl = (IInternalWebControl)control;
        await internalControl.AddAttributesToRender(writer, token);
        writer.MergeAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton wfc-skeleton-input");
        writer.AddAttribute("data-wfc-skeleton", null);
        writer.AddAttribute("aria-hidden", "true");
        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");

        await writer.RenderBeginTagAsync(internalControl.TagName);
        await writer.RenderEndTagAsync();
    }
}
