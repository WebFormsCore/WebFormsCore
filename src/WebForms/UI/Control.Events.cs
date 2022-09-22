using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace System.Web.UI;

public partial class Control
{
    internal int WriteViewState(ref ViewStateWriter writer, HtmlForm form)
    {
        var count = 1;
        
        OnWriteViewState(ref writer);

        foreach (var control in Controls)
        {
            if (control is HtmlForm && control != form)
            {
                continue;
            }

            count += control.WriteViewState(ref writer, form);
        }

        return count;
    }

    internal int ReadViewState(ref ViewStateReader reader, HtmlForm form)
    {
        var count = 1;

        OnReadViewState(ref reader);

        foreach (var control in Controls)
        {
            if (control is HtmlForm && control != form)
            {
                continue;
            }

            count += control.ReadViewState(ref reader, form);
        }

        return count;
    }

    internal void InvokeFrameworkInit(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        
        FrameworkInitialize();
        ViewState.TrackViewState();

        foreach (var control in Controls)
        {
            control.InvokeFrameworkInit(token);
        }
    }

    internal async ValueTask InvokeInitAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        
        OnInit(EventArgs.Empty);
        await OnInitAsync(token);

        foreach (var control in Controls)
        {
            await control.InvokeInitAsync(token);
        }
    }

    internal async ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form)
    {
        if (token.IsCancellationRequested) return;
        
        OnLoad(EventArgs.Empty);
        await OnLoadAsync(token);

        foreach (var control in Controls)
        {
            if (form != null && control is HtmlForm && control != form)
            {
                continue;
            }

            await control.InvokeLoadAsync(token, form);
        }
    }
}