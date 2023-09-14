using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

public static class ControlExtensions
{
    public static async ValueTask<string> RenderToStringAsync(this Control control, CancellationToken token = default)
    {
        await using var subWriter = new StringHtmlTextWriter();

        await control.RenderAsync(subWriter, token);
        await subWriter.FlushAsync();

        return subWriter.ToString();
    }

    public static async ValueTask<string> RenderChildrenToStringAsync(this Control control, CancellationToken token = default)
    {
        await using var subWriter = new StringHtmlTextWriter();

        await control.RenderChildrenInternalAsync(subWriter, token);
        await subWriter.FlushAsync();

        return subWriter.ToString();
    }

    public static T FindControls<T>(this Control control)
        where T : ITemplateControls, new()
    {
        var container = new T();
        container.Load(control);
        return container;
    }

    public static T? FindParent<T>(this Control control)
        where T : Control
    {
        var parent = control.ParentInternal;

        while (parent != null)
        {
            if (parent is T t)
            {
                return t;
            }

            parent = parent.ParentInternal;
        }

        return null;
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

    public static IEnumerable<Control> EnumerateControls(this Control control, Func<Control, bool> filter)
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
        if (this is IDisposable or IAsyncDisposable)
        {
            Page.RegisterDisposable(this);
        }

        if (ProcessControl)
        {
            FrameworkInitialize();
            FrameworkInitialized();

            _state = ControlState.FrameworkInitialized;
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                control.InvokeFrameworkInit(token);
            }
        }
    }

    public void InvokeTrackViewState(bool force = false)
    {
        if (ProcessControl)
        {
            if (!_trackViewState || force)
            {
                TrackViewState(new ViewStateProvider(ServiceProvider));
                _trackViewState = true;
            }
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                control.InvokeTrackViewState(force);
            }
        }
    }

    internal async ValueTask InvokeInitAsync(CancellationToken token)
    {
        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            OnInit(EventArgs.Empty);
            await OnInitAsync(token);

            InvokeTrackViewState();

            _state = ControlState.Initialized;
            _viewState?.TrackViewState();
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                await control.InvokeInitAsync(token);
            }
        }
    }

    internal async ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument)
    {
        IsNew = false;

        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            if (UniqueID is { } uniqueId)
            {
                var didLoad = false;

                if (this is IPostBackAsyncDataHandler asyncDataHandler)
                {
                    didLoad = await asyncDataHandler.LoadPostDataAsync(uniqueId, Page.Request.Form, token);
                }
                else if (this is IPostBackDataHandler dataHandler)
                {
                    didLoad = dataHandler.LoadPostData(uniqueId, Page.Request.Form);
                }

                if (didLoad)
                {
                    var handlers = Page.ChangedPostDataConsumers ??= new List<object>();

                    handlers.Add(this);
                }
            }

            if (target == UniqueID)
            {
                if (this is IPostBackAsyncEventHandler asyncEventHandler)
                {
                    await asyncEventHandler.RaisePostBackEventAsync(argument);
                }
                else if (this is IPostBackEventHandler eventHandler)
                {
                    eventHandler.RaisePostBackEvent(argument);
                }
            }
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                if (!control.IsInPage)
                {
                    continue;
                }

                if (form != null && control is HtmlForm && control != form) continue;

                await control.InvokePostbackAsync(token, form, target, argument);
            }
        }
    }

    internal async ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form)
    {
        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            OnLoad(EventArgs.Empty);
            await OnLoadAsync(token);

            _state = ControlState.Loaded;
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                if (form != null && control is HtmlForm && control != form) continue;

                await control.InvokeLoadAsync(token, form);
            }
        }
    }

    internal async ValueTask InvokePreRenderAsync(CancellationToken token, HtmlForm? form)
    {
        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            OnPreRender(EventArgs.Empty);
            await OnPreRenderAsync(token);

            _state = ControlState.PreRendered;
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                if (form != null && control is HtmlForm && control != form) continue;

                await control.InvokePreRenderAsync(token, form);
            }
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
        InvokeTrackViewState();
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
