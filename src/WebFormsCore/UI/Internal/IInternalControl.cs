using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

/// <summary>
/// Internal methods of <see cref="Control"/> are exposed through this interface to allow the control to be used in a WebForms page.
/// </summary>
public interface IInternalControl
{
    Control Control { get; }

    HttpContext Context { get; }

    IInternalPage Page { get; set; }

    bool IsInPage { get; }

    void FrameworkInit();

    void InvokeTrackViewState(CancellationToken token);

    ValueTask PreInitAsync(CancellationToken token);

    ValueTask InitAsync(CancellationToken token);

    ValueTask PostbackAsync(CancellationToken token, string? target, string? argument);

    ValueTask LoadAsync(CancellationToken token);

    ValueTask PreRenderAsync(CancellationToken token);

    ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token);
}
