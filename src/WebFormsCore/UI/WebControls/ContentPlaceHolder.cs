using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Defines a region in a master page that can be replaced by content from a content page.
/// </summary>
[ParseChildren(false)]
public class ContentPlaceHolder : Control, INamingContainer
{
    internal Content? Content { get; set; }

    protected override void FrameworkInitialized()
    {
        base.FrameworkInitialized();

        // Register with the parent master page
        var parent = ParentInternal;
        while (parent is not null)
        {
            if (parent is MasterPage masterPage)
            {
                masterPage.RegisterContentPlaceHolder(this);
                break;
            }
            parent = parent.ParentInternal;
        }
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (Content is not null)
        {
            await Content.RenderContentAsync(writer, token);
        }
        else
        {
            await base.RenderChildrenAsync(writer, token);
        }
    }
}
