using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

/// <summary>
/// Internal methods of <see cref="WebControl"/> are exposed through this interface to allow API consumers to create custom skeleton renderers.
/// </summary>
public interface IInternalWebControl : IInternalControl
{
    /// <summary>
    /// Gets the <see cref="WebControl"/> associated with this interface.
    /// </summary>
    new WebControl Control { get; }

    /// <summary>
    /// Gets the HTML tag name for the control.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Adds HTML attributes to the specified <see cref="HtmlTextWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="HtmlTextWriter"/> to write attributes to.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token);
}
