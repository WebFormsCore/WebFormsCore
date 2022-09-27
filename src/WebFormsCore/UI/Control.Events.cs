using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

internal static class ControlExtensions
{
    public static IEnumerable<Control> EnumerateControls(this Control control)
    {
        yield return control;

        foreach (var child in control.Controls)
        {
            foreach (var descendant in EnumerateControls(child))
            {
                yield return descendant;
            }
        }
    }

    public static IEnumerable<Control> EnumerateControls(this Control control, Func<Control, bool> filter)
    {
        yield return control;

        foreach (var child in control.Controls)
        {
            if (!filter(child))
            {
                continue;
            }

            foreach (var descendant in EnumerateControls(child, filter))
            {
                yield return descendant;
            }
        }
    }
}

public partial class Control
{
    internal void LoadViewState(ref ViewStateReader reader)
    {
        OnLoadViewState(ref reader);
    }

    internal void WriteViewState(ref ViewStateWriter writer)
    {
        OnWriteViewState(ref writer);
    }

    internal void InvokeFrameworkInit(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        
        FrameworkInitialize();
        FrameworkInitialized();
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

    internal async ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument)
    {
        if (token.IsCancellationRequested) return;

        await OnPostbackAsync(token);

        if (target == UniqueID)
        {
            if (this is IPostBackEventHandler eventHandler)
            {
                eventHandler.RaisePostBackEvent(argument);
            }

            if (this is IPostBackAsyncEventHandler asyncEventHandler)
            {
                await asyncEventHandler.RaisePostBackEventAsync(argument);
            }
        }

        foreach (var control in Controls)
        {
            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokePostbackAsync(token, form, target, argument);
        }
    }

    internal async ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form)
    {
        if (token.IsCancellationRequested) return;

        OnLoad(EventArgs.Empty);
        await OnLoadAsync(token);

        foreach (var control in Controls)
        {
            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokeLoadAsync(token, form);
        }
    }

    internal async ValueTask InvokePreRenderAsync(CancellationToken token, HtmlForm? form)
    {
        if (token.IsCancellationRequested) return;

        OnPreRender(EventArgs.Empty);
        await OnPreRenderAsync(token);

        foreach (var control in Controls)
        {
            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokePreRenderAsync(token, form);
        }
    }
}
