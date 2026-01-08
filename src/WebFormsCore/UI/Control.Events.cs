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

    public static T? FindControl<T>(this Control control, string id)
        where T : Control
    {
        return control.FindControl(id) as T;
    }

    public static T FindRequiredControl<T>(this Control control, string id)
        where T : Control
    {
        return control.FindControl(id) as T ?? throw new InvalidOperationException($"Control with ID '{id}' not found.");
    }

    public static Control FindRequiredControl(this Control control, string id)
    {
        return control.FindControl(id) ?? throw new InvalidOperationException($"Control with ID '{id}' not found.");
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
                    if (_filter == null || _filter(currentControl))
                    {
                        return true;
                    }

                    continue;
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

    internal async ValueTask FrameworkInitAsync(CancellationToken token)
    {
        if (_state >= ControlState.FrameworkInitialized)
        {
            return;
        }

        if (token.IsCancellationRequested) return;

        await OnFrameworkInitAsync(token);

        _state = ControlState.FrameworkInitialized;
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

    internal async ValueTask PreInitAsync(CancellationToken token)
    {
        if (_state >= ControlState.PreInitialized) return;

        if (token.IsCancellationRequested) return;

        await OnPreInitAsync(token);

        if (ProcessControl)
        {
            await PreInit.InvokeAsync(this, EventArgs.Empty);

            _state = ControlState.PreInitialized;
        }
    }

    internal async ValueTask InitAsync(CancellationToken token)
    {
        if (_state >= ControlState.Initialized) return;

        if (token.IsCancellationRequested) return;

        await OnInitAsync(token);

        if (ProcessControl)
        {
            await Init.InvokeAsync(this, EventArgs.Empty);
            InvokeTrackViewState();

            _state = ControlState.Initialized;
            _viewState?.TrackViewState();
        }
    }

    internal async ValueTask UnloadAsync(CancellationToken token)
    {
        await OnUnloadAsync(token);

        if (ProcessControl)
        {
            // Unload event doesn't exist currently, but we follow the pattern
        }
    }

    internal async ValueTask PostbackAsync(CancellationToken token)
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
            var form = Page.ActiveForm;

            foreach (var control in Controls)
            {
                if (!control.IsInPage)
                {
                    continue;
                }

                if (form != null && control is HtmlForm && control != form) continue;

                await control.PostbackAsync(token);
            }
        }
    }

    internal async ValueTask LoadAsync(CancellationToken token)
    {
        if (_state >= ControlState.Loaded) return;

        if (token.IsCancellationRequested) return;

        await OnLoadAsync(token);

        if (ProcessControl)
        {
            await Load.InvokeAsync(this, EventArgs.Empty);

            if (!Page.IsPostBack) RegisterBackgroundControl();

            _state = ControlState.Loaded;
        }
    }

    internal void InvokeRegisterBackgroundControl(CancellationToken token)
    {
        if (ProcessControl)
        {
            RegisterBackgroundControl();
        }

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                if (token.IsCancellationRequested) return;

                control.InvokeRegisterBackgroundControl(token);
            }
        }
    }

    private void RegisterBackgroundControl()
    {
        if (this is not IBackgroundLoadHandler backgroundLoadAsyncHandler) return;

        var task = backgroundLoadAsyncHandler.OnBackgroundLoadAsync();

        if (!task.IsCompletedSuccessfully)
        {
            Page.RegisterAsyncTask(task);
        }
    }

    internal async ValueTask PreRenderAsync(CancellationToken token)
    {
        if (_state >= ControlState.PreRendered) return;

        if (token.IsCancellationRequested) return;

        await OnPreRenderAsync(token);

        if (ProcessControl)
        {
            await PreRender.InvokeAsync(this, EventArgs.Empty);

            _state = ControlState.PreRendered;
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

    ValueTask IInternalControl.FrameworkInitAsync(CancellationToken token)
    {
        return FrameworkInitAsync(token);
    }

    void IInternalControl.InvokeTrackViewState(CancellationToken token)
    {
        InvokeTrackViewState();
    }

    ValueTask IInternalControl.PreInitAsync(CancellationToken token)
    {
        return PreInitAsync(token);
    }

    ValueTask IInternalControl.InitAsync(CancellationToken token)
    {
        return InitAsync(token);
    }

    ValueTask IInternalControl.PostbackAsync(CancellationToken token, string? target, string? argument)
    {
        return PostbackAsync(token);
    }

    ValueTask IInternalControl.LoadAsync(CancellationToken token)
    {
        return LoadAsync(token);
    }

    ValueTask IInternalControl.PreRenderAsync(CancellationToken token)
    {
        return PreRenderAsync(token);
    }

    #endregion
}
