using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

public interface IInternalControl
{
    void InvokeFrameworkInit(CancellationToken token);

    void InvokeTrackViewState(CancellationToken token);

    ValueTask InvokeInitAsync(CancellationToken token);

    ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument);

    ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form);

    ValueTask InvokePreRenderAsync(CancellationToken token, HtmlForm? form);

    Task RenderAsync(HtmlTextWriter writer, CancellationToken token);
}
