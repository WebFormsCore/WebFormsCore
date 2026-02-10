using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton.Renderers;

/// <summary>
/// Skeleton renderer for <see cref="Button"/> controls.
/// Renders a button-shaped skeleton placeholder.
/// </summary>
public class ButtonSkeletonRenderer : ISkeletonRenderer<Button>
{
    public async ValueTask RenderSkeletonAsync(Button control, HtmlTextWriter writer, CancellationToken token)
    {
        var hasContent = !string.IsNullOrEmpty(control.Text) || control.HasControls();

        if (control.IsInPage)
        {
            var internalControl = (IInternalWebControl)control;
            await internalControl.AddAttributesToRender(writer, token);
        }

        if (!hasContent) writer.MergeAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton wfc-skeleton-button");
        writer.AddAttribute("data-wfc-skeleton", null);
        writer.AddAttribute("aria-hidden", "true");
        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Button);

        if (!hasContent)
        {
            await writer.WriteAsync("&nbsp;");
        }
        else if (control.HasControls())
        {
            await control.RenderChildrenInternalAsync(writer, token);
        }
        else
        {
            await writer.WriteAsync(control.Text);
        }

        await writer.RenderEndTagAsync();
    }
}
