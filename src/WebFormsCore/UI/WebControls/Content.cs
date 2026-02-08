using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Contains content to be rendered within a <see cref="ContentPlaceHolder"/> in a master page.
/// </summary>
[ParseChildren(false)]
public class Content : Control
{
    /// <summary>
    /// Gets or sets the ID of the <see cref="ContentPlaceHolder"/> that this content targets.
    /// </summary>
    public string? ContentPlaceHolderID { get; set; }

    /// <summary>
    /// Content controls do not render themselves. Their children are rendered
    /// by the matching <see cref="ContentPlaceHolder"/> in the master page.
    /// </summary>
    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return default;
    }

    /// <summary>
    /// Renders the children of this content control. Called by <see cref="ContentPlaceHolder"/>.
    /// </summary>
    internal ValueTask RenderContentAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return base.RenderChildrenAsync(writer, token);
    }
}
