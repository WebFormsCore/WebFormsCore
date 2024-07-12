using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

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

        return default;
    }

    public static T? FindDataItem<T>(this Control control)
    {
        var parent = control.ParentInternal;

        while (parent != null)
        {
            if (parent is IDataItemContainer { DataItem: T t })
            {
                return t;
            }

            parent = parent.ParentInternal;
        }

        return default;
    }

    public static ControlEnumerable EnumerateControls(this Control control, Func<Control, bool> filter, int depth = 512)
    {
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));

        return new ControlEnumerable(control, filter, depth);
    }

    public static ControlEnumerable EnumerateControls(this Control control, int depth = 512)
    {
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));

        return new ControlEnumerable(control, null, depth);
    }

    public readonly struct ControlEnumerable(Control control, Func<Control, bool>? filter, int depth) : IEnumerable<Control>
    {
        public ControlEnumerator GetEnumerator()
        {
            return new ControlEnumerator(control, filter, depth);
        }

        IEnumerator<Control> IEnumerable<Control>.GetEnumerator()
        {
            return new ControlEnumerator(control, filter, depth);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct ControlEnumerator : IEnumerator<Control>
    {
        private readonly (Control Control, int Index)[] _array;
        private readonly Func<Control, bool>? _filter;
        private int _currentIndex = -1;

        public ControlEnumerator(Control root, Func<Control, bool>? filter, int depth = 512)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _filter = filter;

            _array = ArrayPool<(Control, int)>.Shared.Rent(depth);
            Push(root, -2);
        }

        public void Reset()
        {
            _currentIndex = -1;
        }

        object IEnumerator.Current => Current;

        public Control Current => _array[_currentIndex].Control;

        public bool MoveNext()
        {
            while (_currentIndex >= 0)
            {
                var (currentControl, index) = _array[_currentIndex--];

                index++;

                if (index >= (currentControl.HasControls() ? currentControl.Controls.Count : 0))
                {
                    continue;
                }

                Push(currentControl, index);

                if (index < 0)
                {
                    return true;
                }

                var nextControl = currentControl.Controls[index];
                Push(nextControl, -1);

                if (_filter == null || _filter(nextControl))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(Control control, int index)
        {
            _array[++_currentIndex] = (control, index);
        }

        public void Dispose()
        {
            ArrayPool<(Control, int)>.Shared.Return(_array, true);
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
        if (_state >= ControlState.FrameworkInitialized)
        {
            return;
        }

        _state = ControlState.FrameworkInitialized;

        FrameworkInitialize();
        FrameworkInitialized();

        foreach (var control in Controls)
        {
            control.InvokeFrameworkInit(token);
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

    internal void InvokeUnload()
    {
        if (ProcessControl)
        {
            OnUnload(EventArgs.Empty);
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                control.InvokeUnload();
            }
        }
    }

    internal async ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form)
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

                await control.InvokePostbackAsync(token, form);
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

    public virtual async ValueTask DataBindAsync(CancellationToken token = default)
    {
        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            await DataBinding.InvokeAsync(this, EventArgs.Empty);
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                await control.DataBindAsync(token);
            }
        }
    }

    protected async ValueTask InvokeDataBindingAsync(CancellationToken token)
    {
        if (ProcessControl)
        {
            if (token.IsCancellationRequested) return;

            await DataBinding.InvokeAsync(this, EventArgs.Empty);
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

    HttpContext IInternalControl.Context => Context;

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
        return InvokePostbackAsync(token, form);
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
