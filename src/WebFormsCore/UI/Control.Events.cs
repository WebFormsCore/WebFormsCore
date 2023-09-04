using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

public static class ControlExtensions
{
    public static T FindControls<T>(this Control control)
        where T : ITemplateControls, new()
    {
        var container = new T();
        container.Load(control);
        return container;
    }

    public static IEnumerable<Control> EnumerateControls(this Control control)
    {
        yield return control;

        if (!control.HasControls()) yield break;

        foreach (var child in control.Controls)
        {
            foreach (var descendant in EnumerateControls(child))
            {
                yield return descendant;
            }
        }
    }

    public  static IEnumerable<Control> EnumerateControls(this Control control, Func<Control, bool> filter)
    {
        yield return control;

        if (!control.HasControls()) yield break;

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

public partial class Control : IInternalControl
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
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        if (this is IDisposable or IAsyncDisposable)
        {
            Page.RegisterDisposable(this);
        }

        FrameworkInitialize();
        FrameworkInitialized();

        _state = ControlState.FrameworkInitialized;

        foreach (var control in Controls)
        {
            control.InvokeFrameworkInit(token);
        }
    }

    internal void InvokeTrackViewState(CancellationToken token)
    {
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        TrackViewState(new ViewStateProvider(ServiceProvider));

        foreach (var control in Controls)
        {
            control.InvokeTrackViewState(token);
        }
    }

    internal async ValueTask InvokeInitAsync(CancellationToken token)
    {
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        OnInit(EventArgs.Empty);
        await OnInitAsync(token);

        InvokeTrackViewState(token);

        _state = ControlState.Initialized;
        ViewState.TrackViewState();

        for (var i = 0; i < Controls.Count; i++)
        {
            var control = Controls[i];

            await control.InvokeInitAsync(token);
        }
    }

    internal async ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument)
    {
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        if (UniqueID is {} uniqueId)
        {
            var didLoad = false;

            if (this is IPostBackDataHandler dataHandler)
            {
                didLoad = dataHandler.LoadPostData(uniqueId, Page.Request.Form);
            }

            if (this is IPostBackAsyncDataHandler asyncDataHandler)
            {
                didLoad = await asyncDataHandler.LoadPostDataAsync(uniqueId, Page.Request.Form, token);
            }

            if (didLoad)
            {
                var handlers = Page._changedPostDataConsumers ??= new List<object>();

                handlers.Add(this);
            }
        }

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

        for (var i = 0; i < Controls.Count; i++)
        {
            var control = Controls[i];

            if (!control.IsInPage)
            {
                continue;
            }

            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokePostbackAsync(token, form, target, argument);
        }
    }

    internal async ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form)
    {
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        OnLoad(EventArgs.Empty);
        await OnLoadAsync(token);

        _state = ControlState.Loaded;

        for (var i = 0; i < Controls.Count; i++)
        {
            var control = Controls[i];

            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokeLoadAsync(token, form);
        }
    }

    internal async ValueTask InvokePreRenderAsync(CancellationToken token, HtmlForm? form)
    {
        if (!ProcessControl) return;
        if (token.IsCancellationRequested) return;

        OnPreRender(EventArgs.Empty);
        await OnPreRenderAsync(token);

        _state = ControlState.PreRendered;

        for (var i = 0; i < Controls.Count; i++)
        {
            var control = Controls[i];

            if (form != null && control is HtmlForm && control != form) continue;

            await control.InvokePreRenderAsync(token, form);
        }
    }

    #region IInternalControl

    IInternalPage IInternalControl.Page
    {
        get => Page;
        set
        {
            _page = (Page)value;
            _form = null;
        }
    }

    bool IInternalControl.IsInPage => IsInPage;

    Control IInternalControl.Control => this;

    IHttpContext IInternalControl.Context => Context;

    void IInternalControl.InvokeFrameworkInit(CancellationToken token)
    {
        InvokeFrameworkInit(token);
    }

    void IInternalControl.InvokeTrackViewState(CancellationToken token)
    {
        InvokeTrackViewState(token);
    }

    ValueTask IInternalControl.InvokeInitAsync(CancellationToken token)
    {
        return InvokeInitAsync(token);
    }

    ValueTask IInternalControl.InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument)
    {
        return InvokePostbackAsync(token, form, target, argument);
    }

    ValueTask IInternalControl.InvokeLoadAsync(CancellationToken token, HtmlForm? form)
    {
        return InvokeLoadAsync(token, form);
    }

    ValueTask IInternalControl.InvokePreRenderAsync(CancellationToken token, HtmlForm? form)
    {
        return InvokePreRenderAsync(token, form);
    }

    #endregion
}
