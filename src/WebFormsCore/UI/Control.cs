using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;
using System.Collections.Frozen;

namespace WebFormsCore.UI;

public delegate Task RenderAsyncDelegate(HtmlTextWriter writer, ControlCollection controls, CancellationToken token);

public enum ControlState
{
    Constructed,
    FrameworkInitialized,
    PreInitialized,
    Initialized,
    Loaded,
    PreRendered,
}

public partial class Control
#pragma warning disable CS0436 // Type conflicts with imported type
    : System.Web.UI.Control
#pragma warning restore CS0436
{
    protected const char IdSeparator = '$';

    private StateBag? _viewState;
    private ControlCollection? _controls;
    private Control? _namingContainer;
    private string? _id;
    private bool _hasGeneratedId;
    private bool _forceClientIdAttribute;
    private bool _generatedIdChanged;
    private string? _cachedUniqueID;
    private string? _cachedPredictableID;
    private Control? _parent;
    private OccasionalFields? _occasionalFields;
    private Page? _page;
    private HtmlForm? _form;
    private IWebObjectActivator? _webObjectActivator;
    private RenderAsyncDelegate? _renderMethod;
    private bool _visible = true;
    private bool _visibleTracking;
    private bool _trackViewState;
    protected internal ControlState _state = ControlState.Constructed;
    private bool? _enableViewState;
    private string? _clientId;

    public event AsyncEventHandler<Control, EventArgs>? PreInit;
    public event AsyncEventHandler<Control, EventArgs>? Init;
    public event AsyncEventHandler<Control, EventArgs>? Load;
    public event AsyncEventHandler<Control, EventArgs>? PreRender;

    protected bool IsTrackingViewState => _trackViewState;

    public IWebObjectActivator WebActivator => _webObjectActivator ??= ServiceProvider.GetRequiredService<IWebObjectActivator>();

    public event AsyncEventHandler? DataBinding;

    protected virtual IServiceProvider ServiceProvider => Page.ServiceProvider;

    public virtual bool EnableViewState
    {
        get
        {
            var current = this;

            while (current != null)
            {
                if (current._enableViewState.HasValue)
                {
                    return current._enableViewState.Value;
                }

                current = current._parent;
            }

            return true;
        }
        set => _enableViewState = value;
    }

    protected virtual bool EnableViewStateBag => EnableViewState;

    protected virtual bool AddClientIdToAttributes => (_id is not null && !_hasGeneratedId) || _forceClientIdAttribute;

    protected virtual bool ProcessControl => true;

    public bool IsNew { get; internal set; } = true;

    protected virtual bool ProcessChildren => ProcessControl && _controls is not null && _controls.Count > 0;

    internal bool ProcessControlInternal => ProcessControl;

    internal bool ProcessChildrenInternal => ProcessChildren;

    /// <summary>Gets a reference to the <see cref="T:System.Web.UI.Page" /> instance that contains the server control.</summary>
    /// <returns>The <see cref="T:System.Web.UI.Page" /> instance that contains the server control.</returns>
    /// <exception cref="T:System.InvalidOperationException">The control is a <see cref="T:System.Web.UI.WebControls.Substitution" /> control.</exception>
    [Bindable(false)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual Page Page
    {
        get
        {
            if (_page == null)
            {
                if (this is Page page)
                {
                    _page = page;
                }
                else if (_parent != null)
                {
                    _page = _parent.Page;
                }
            }

            return _page ?? throw new InvalidOperationException("Control is not added to a page.");
        }
    }

    /// <summary>Gets a reference to the <see cref="T:System.Web.UI.Page" /> instance that contains the server control.</summary>
    /// <returns>The <see cref="T:System.Web.UI.Page" /> instance that contains the server control.</returns>
    /// <exception cref="T:System.InvalidOperationException">The control is a <see cref="T:System.Web.UI.WebControls.Substitution" /> control.</exception>
    [Bindable(false)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual HtmlForm? Form
    {
        get
        {
            if (_form == null && _parent != null)
            {
                if (_parent is HtmlForm form)
                {
                    _form = form;
                }
                else
                {
                    _form = _parent.Form;
                }
            }

            return _form;
        }
    }

    /// <summary>Gets a reference to the server control's parent control in the page control hierarchy.</summary>
    /// <returns>A reference to the server control's parent control.</returns>
    [Bindable(false)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control Parent => _parent ?? throw new InvalidOperationException("Control is not added to a parent.");

    internal Control? ParentInternal => _parent;

    /// <summary>Gets or sets the programmatic identifier assigned to the server control.</summary>
    /// <returns>The programmatic identifier assigned to the control.</returns>
    [ParenthesizePropertyName(true)]
    [MergableProperty(false)]
    public virtual string? ID
    {
        get => _id;
        set
        {
            if (value == "") value = null;

            var id = _id;
            var updateGeneratedIds = _hasGeneratedId;

            _id = value;
            _hasGeneratedId = false;

            if (_namingContainer != null && id != null)
            {
                _namingContainer.DirtyNameTable();
            }

            if (id != null && id != _id)
            {
                ClearCachedClientID();
                ClearCachedUniqueIDRecursive();
            }

            if (updateGeneratedIds)
            {
                _namingContainer?.UpdateGeneratedIds();
            }
        }
    }

    public virtual bool Visible
    {
        get => _visible && (_parent == null || _parent.Visible);
        set
        {
            _visible = value;
            _visibleTracking = ViewState.IsTracking;
        }
    }

    public bool SelfVisible => _visible;

    public virtual string? AppRelativeVirtualPath => null;

    public virtual string? AppFullPath => null;

    public virtual string? TemplateSourceDirectory => null;

    /// <summary>Gets the unique, hierarchically qualified identifier for the server control.</summary>
    /// <returns>The fully qualified identifier for the server control.</returns>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual string? UniqueID
    {
        get
        {
            if (_cachedUniqueID != null)
            {
                return _cachedUniqueID;
            }

            var namingContainer = NamingContainer;
            if (namingContainer == null)
            {
                return _hasGeneratedId ? null : _id;
            }

            if (_id is null && GenerateAutomaticID)
            {
                namingContainer.UpdateGeneratedIds();
            }

            if (Page == namingContainer || _id is null)
            {
                _cachedUniqueID = _id;
            }
            else
            {
                var uniqueIdPrefix = namingContainer.GetUniqueIDPrefix();
                if (uniqueIdPrefix.Length == 0) return _id;
                _cachedUniqueID = uniqueIdPrefix + _id;
            }
            return _cachedUniqueID;
        }
    }

    private ClientIDMode ClientIDModeValue { get; set; }

    /// <summary>Gets or sets the algorithm that is used to generate the value of the <see cref="P:WebFormsCore.UI.Control.ClientID" /> property.</summary>
    /// <returns>A value that indicates how the <see cref="P:WebFormsCore.UI.Control.ClientID" /> property is generated. The default is <see cref="F:WebFormsCore.ClientIDMode.Inherit" />.</returns>
    [DefaultValue(ClientIDMode.Inherit)]
    public virtual ClientIDMode ClientIDMode
    {
        get => ClientIDModeValue;
        set
        {
            if (ClientIDModeValue == value)
                return;
            if (value != EffectiveClientIDModeValue)
            {
                ClearEffectiveClientIDMode();
                ClearCachedClientID();
            }
            ClientIDModeValue = value;
        }
    }

    public ClientIDMode EffectiveClientIDModeValue { get; private set; }

    internal virtual ClientIDMode EffectiveClientIDMode
    {
        get
        {
            if (EffectiveClientIDModeValue == ClientIDMode.Inherit)
            {
                EffectiveClientIDModeValue = ClientIDMode;
                if (EffectiveClientIDModeValue == ClientIDMode.Inherit)
                {
                    if (NamingContainer != null)
                    {
                        EffectiveClientIDModeValue = NamingContainer.EffectiveClientIDMode;
                    }
                    else
                    {
                        EffectiveClientIDModeValue = ClientIDMode.AutoID;
                    }
                }
            }
            return EffectiveClientIDModeValue;
        }
    }

    /// <summary>Gets the control ID for HTML markup that is generated by ASP.NET.</summary>
    /// <returns>The control ID for HTML markup that is generated by ASP.NET.</returns>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual string? ClientID
    {
        get
        {
            _forceClientIdAttribute = true;
            return _clientId ?? EffectiveClientIDMode switch
            {
                ClientIDMode.Predictable => PredictableClientID,
                ClientIDMode.Static => ID,
                ClientIDMode.Hidden => null,
                _ => UniqueClientID
            };
        }
        set
        {
            _forceClientIdAttribute = true;
            _clientId = value;
        }
    }

    internal string? UniqueClientID
    {
        get
        {
            var uniqueId = UniqueID;

            return uniqueId != null && uniqueId.IndexOf(IdSeparator) >= 0
                ? uniqueId.Replace(IdSeparator, '_')
                : uniqueId;
        }
    }

    internal string? PredictableClientID => _cachedPredictableID ??= GetPredictableClientIDPrefix();

    /// <summary>Gets a reference to the server control's naming container, which creates a unique namespace for differentiating between server controls with the same <see cref="P:WebFormsCore.UI.Control.ID" /> property value.</summary>
    /// <returns>The server control's naming container.</returns>
    [Bindable(false)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual Control? NamingContainer => _namingContainer ??= _parent is INamingContainer ? _parent : _parent?.NamingContainer;

    /// <summary>Gets the <see cref="T:System.Web.HttpContext" /> object associated with the server control for the current Web request.</summary>
    /// <returns>The specified <see cref="T:System.Web.HttpContext" /> object associated with the current request.</returns>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual HttpContext Context => Page.Context ?? throw new InvalidOperationException("HttpContext is not available");

    protected HttpRequest Request => Context.Request;

    protected HttpResponse Response => Context.Response;

    public virtual void ClearControl()
    {
        EffectiveClientIDModeValue = ClientIDMode.Inherit;
        ClientIDModeValue = ClientIDMode.Inherit;
        _viewState = null;
        _controls = null;
        _namingContainer = null;
        _id = null;
        _hasGeneratedId = false;
        _cachedUniqueID = null;
        _cachedPredictableID = null;
        _parent = null;
        _occasionalFields = null;
        _page = null;
        _form = null;
        _webObjectActivator = null;
        _renderMethod = null;
        _visible = true;
        _visibleTracking = false;
        _trackViewState = false;
        _enableViewState = null;
        _forceClientIdAttribute = false;
        _state = ControlState.Constructed;
        _clientId = null;

        Init = null;
        Load = null;

    }

    protected virtual string GetUniqueIDPrefix()
    {
        if (OccasionalFields.UniqueIDPrefix == null)
        {
            var uniqueId = UniqueID;
            OccasionalFields.UniqueIDPrefix = string.IsNullOrEmpty(uniqueId) ? string.Empty : uniqueId + IdSeparator;
        }

        return OccasionalFields.UniqueIDPrefix;
    }

    /// <summary>Gets a <see cref="T:WebFormsCore.UI.ControlCollection" /> object that represents the child controls for a specified server control in the UI hierarchy.</summary>
    /// <returns>The collection of child controls for the specified server control.</returns>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual ControlCollection Controls
    {
        get => _controls ??= CreateControlCollection();
        set
        {
            if (value.Count == 0)
            {
                return;
            }

            value.CheckOwnerNotSet();

            if (_controls is null or { Count: 0 })
            {
                _controls = value;
            }
            else
            {
                value.SetCollectionReadOnly("Cannot re-use ControlCollection instance after it has been assigned to a Control.");
                _controls.AddRangeDangerously(value);
            }

            value.SetOwner(this);
        }
    }

    public virtual bool HasControls() => _controls is { Count: > 0 };

    public virtual bool HasRenderingData() => HasControls() || HasRenderDelegate();

    public bool HasRenderDelegate() => _renderMethod is not null;

    /// <summary>Gets a dictionary of state information that allows you to save and restore the view state of a server control across multiple requests for the same page.</summary>
    /// <returns>An instance of the <see cref="T:WebFormsCore.UI.StateBag" /> class that contains the server control's view-state information.</returns>
    protected virtual StateBag ViewState
    {
        get
        {
            if (_viewState != null) return _viewState;
            _viewState = new StateBag(ViewStateIgnoresCase);

            if (_trackViewState)
            {
                _viewState.TrackViewState();
            }

            return _viewState;
        }
    }

    private OccasionalFields OccasionalFields => _occasionalFields ??= new OccasionalFields();

    /// <summary>Gets a value that indicates whether the <see cref="T:WebFormsCore.UI.StateBag" /> object is case-insensitive.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:WebFormsCore.UI.StateBag" /> instance is case-insensitive; otherwise, <see langword="false" />. The default is <see langword="false" />.</returns>
    protected virtual bool ViewStateIgnoresCase => false;

    /// <summary>
    /// True if the control is added to the page.
    /// </summary>
    internal bool IsInPage => this is Page || _page is not null || _parent is { IsInPage: true };

    /// <summary>Sends server control content to a provided <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object, which writes the content to be rendered on the client.</summary>
    /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object that receives the server control content. </param>
    /// <param name="token"></param>
    public virtual async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (Visible)
        {
            await RenderChildrenAsync(writer, token);
        }
    }

    public void SetRenderMethodDelegate(RenderAsyncDelegate renderMethod)
    {
        _renderMethod = renderMethod;
    }

    /// <summary>Outputs the content of a server control's children to a provided <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object, which writes the content to be rendered on the client.</summary>
    /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object that receives the rendered content. </param>
    /// <param name="token"></param>
    protected virtual async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (_renderMethod != null)
        {
            await _renderMethod(writer, Controls, token);
            return;
        }

        if (_controls == null || _controls.Count == 0 || token.IsCancellationRequested) return;

        foreach (var control in _controls)
        {
            await control.RenderAsync(writer, token);
        }
    }
    internal ValueTask RenderChildrenInternalAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return RenderChildrenAsync(writer, token);
    }

    /// <summary>Creates a new <see cref="T:WebFormsCore.UI.ControlCollection" /> object to hold the child controls (both literal and server) of the server control.</summary>
    /// <returns>A <see cref="T:WebFormsCore.UI.ControlCollection" /> object to contain the current server control's child server controls.</returns>
    protected virtual ControlCollection CreateControlCollection()
    {
        return new ControlCollection(this);
    }

    internal void AddedControlInternal(Control control, bool isAddedLast)
    {
        if (control._parent == this)
        {
            return;
        }

        control._parent?.Controls.Remove(control);
        control._parent = this;
        control._page = _page ?? this as Page;
        control._form = _form;

        var namingContainer = this is INamingContainer ? this : NamingContainer;

        if (namingContainer is null)
        {
            return;
        }

        control.UpdateNamingContainer(namingContainer);
        
        // Recursively update naming container for children that were added before this control had a naming container
        if (control.HasControls())
        {
            control.UpdateChildNamingContainers(namingContainer);
        }

        if (control._id == null)
        {
            if (control.GenerateAutomaticID)
            {
                if (isAddedLast && !control.HasControls())
                {
                    control._id = GetId(namingContainer.OccasionalFields.NextIndex);
                    control._hasGeneratedId = true;
                    namingContainer.DirtyNameTable();
                    namingContainer.OccasionalFields.NextIndex++;
                }
                else
                {
                    namingContainer.UpdateGeneratedIds();
                }
            }
        }
        else if (control._id != null || control._controls != null)
        {
            namingContainer.DirtyNameTable();
        }

        if (_state >= ControlState.FrameworkInitialized)
        {
            control.InvokeFrameworkInit();
        }
    }
    
    private void UpdateChildNamingContainers(Control parentNamingContainer)
    {
        foreach (var child in Controls)
        {
            // If child is a naming container, it becomes its own naming container for its children
            var childNamingContainer = child is INamingContainer ? child : parentNamingContainer;
            
            if (child._namingContainer == null)
            {
                child.UpdateNamingContainer(childNamingContainer);
                
                // Mark the naming container's name table as dirty so it will be rebuilt
                if (child._id != null)
                {
                    parentNamingContainer.DirtyNameTable();
                }
            }
            
            if (child.HasControls())
            {
                child.UpdateChildNamingContainers(childNamingContainer);
            }
        }
    }

    internal async ValueTask InvokeStateMethodsAsync(ControlState state, Control control)
    {
        if (state >= ControlState.FrameworkInitialized && control._state < ControlState.FrameworkInitialized)
        {
            control.InvokeFrameworkInit();
        }

        if (state >= ControlState.PreInitialized && control._state < ControlState.PreInitialized)
        {
            await control.PreInitAsync(default);
        }

        if (state >= ControlState.Initialized && control._state < ControlState.Initialized)
        {
            await control.InitAsync(default);
        }

        if (state >= ControlState.Loaded && control._state < ControlState.Loaded)
        {
            await control.LoadAsync(default);
        }

        if (state >= ControlState.PreRendered && control._state < ControlState.PreRendered)
        {
            await control.PreRenderAsync(default);
        }
    }

    internal void ClearNamingContainer()
    {
        DirtyNameTable();
    }

    protected internal virtual void RemovedControlInternal(Control control)
    {
        var unloadTask = control.UnloadAsync(CancellationToken.None);

        if (!unloadTask.IsCompletedSuccessfully)
        {
            Page.ScopedContainer.RegisterTask(unloadTask.AsTask());
        }

        if (control._hasGeneratedId)
        {
            control._id = null;
            control._hasGeneratedId = false;
            control._namingContainer?.UpdateGeneratedIds();
        }

        control._parent = null;
        control._page = null;
        control._form = null;
        control._namingContainer = null;
    }

    private void UpdateNamingContainer(Control namingContainer)
    {
        if (IsInPage && this is IDisposable or IAsyncDisposable)
        {
            Page.RegisterDisposable(this);
        }

        if (_namingContainer == null || _namingContainer != null && _namingContainer != namingContainer)
        {
            ClearCachedUniqueIDRecursive();
        }

        if (EffectiveClientIDModeValue != ClientIDMode.Inherit)
        {
            ClearCachedClientID();
            ClearEffectiveClientIDMode();
        }

        _namingContainer?.Controls.RemoveNamingContainerChild(this);
        _namingContainer = namingContainer;
        _namingContainer.Controls.AddNamingContainerChild(this);
    }

    private string? GetPredictableClientIDPrefix()
    {
        var namingContainer = NamingContainer;
        string? predictableClientIdPrefix;

        if (namingContainer != null)
        {
            namingContainer.UpdateGeneratedIds();

            switch (namingContainer)
            {
                case Page _:
                case MasterPage _:
                    predictableClientIdPrefix = _id;
                    break;
                default:
                    predictableClientIdPrefix = namingContainer.ClientID;

                    if (string.IsNullOrEmpty(predictableClientIdPrefix))
                    {
                        predictableClientIdPrefix = _id;
                        break;
                    }

                    if (!string.IsNullOrEmpty(_id) && this is not IDataItemContainer)
                    {
                        predictableClientIdPrefix = predictableClientIdPrefix + "_" + _id;
                    }

                    break;
            }
        }
        else
        {
            predictableClientIdPrefix = _id;
        }
        return predictableClientIdPrefix;
    }

    protected virtual bool GenerateAutomaticID => true;

    internal void UpdateGeneratedIds()
    {
        var index = 0;
        UpdateGeneratedIds(ref index, Controls);
        DirtyNameTable();
        OccasionalFields.NextIndex = index;
    }

    private static void UpdateGeneratedIds(ref int index, ControlCollection collection)
    {
        foreach (var control in collection)
        {
            if (!control.GenerateAutomaticID || (control._id is not null && !control._hasGeneratedId))
            {
                continue;
            }

            var newId = GetId(index);

            control._generatedIdChanged = control._id != newId;
            control._id = newId;
            control._hasGeneratedId = true;

            index++;
        }

        foreach (var control in collection)
        {
            if (control is not INamingContainer)
            {
                if (control.HasControls())
                {
                    UpdateGeneratedIds(ref index, control.Controls);
                }
            }
            else if (control._generatedIdChanged)
            {
                control.UpdateGeneratedIds();
            }

            control._generatedIdChanged = false;
        }
    }

    internal static string GetId(int index)
    {
        return index >= 128 ? "c" + index.ToString(NumberFormatInfo.InvariantInfo) : AutomaticIDs[index];
    }

    private void ClearCachedUniqueIDRecursive()
    {
        _cachedUniqueID = null;

        if (_occasionalFields != null)
        {
            _occasionalFields.UniqueIDPrefix = null;
        }

        if (_controls == null) return;

        foreach (var control in _controls)
        {
            control.ClearCachedUniqueIDRecursive();
        }
    }

    protected void ClearEffectiveClientIDMode()
    {
        EffectiveClientIDModeValue = ClientIDMode.Inherit;
        if (_controls == null) return;

        foreach (var control in _controls)
        {
            control.ClearEffectiveClientIDMode();
        }
    }

    /// <summary>Sets the cached <see cref="P:WebFormsCore.UI.Control.ClientID" /> value to <see langword="null" />.</summary>
    protected void ClearCachedClientID()
    {
        _cachedPredictableID = null;

        if (_controls == null) return;

        foreach (var control in _controls)
        {
            control.ClearCachedClientID();
        }
    }

    internal void DirtyNameTable()
    {
        OccasionalFields.NamedControls = null;
    }

    /// <summary>
    /// Determines whether the event for the control should be passed up the page's control hierarchy.
    /// </summary>
    protected virtual ValueTask<bool> OnBubbleEventAsync(object source, EventArgs args)
    {
        return default;
    }

    /// <summary>
    ///  Assigns an sources of the event and its information up the page control
    ///  hierarchy until they reach the top of the control tree
    /// </summary>
    protected async ValueTask RaiseBubbleEventAsync(object source, EventArgs args)
    {
        var currentTarget = ParentInternal;

        while (currentTarget != null)
        {
            if (await currentTarget.OnBubbleEventAsync(source, args))
            {
                return;
            }

            currentTarget = currentTarget.ParentInternal;
        }
    }

    /// <summary>Searches the current naming container for a server control with the specified <paramref name="id" /> parameter.</summary>
    /// <param name="id">The identifier for the control to be found. </param>
    /// <returns>The specified control, or <see langword="null" /> if the specified control does not exist.</returns>
    public virtual Control? FindControl(string id)
    {
        if (this is not INamingContainer)
        {
            return NamingContainer?.FindControl(id);
        }

        if (!HasControls())
        {
            return null;
        }

        if (OccasionalFields.NamedControls is not { } namedControls)
        {
            namedControls = new Dictionary<string, Control>();
            FillNamedControls(namedControls, Controls);
            OccasionalFields.NamedControls = namedControls;
        }

        if (namedControls.TryGetValue(id, out var control))
        {
            return control;
        }

        return FindControlByUniqueId(id, Controls);
    }

    private static Control? FindControlByUniqueId(string id, ControlCollection controls)
    {
        foreach (var control in controls)
        {
            if (control.UniqueID == id)
            {
                return control;
            }

            if (control is INamingContainer)
            {
                var uniqueIdPrefix = control.GetUniqueIDPrefix();

                if (!id.StartsWith(uniqueIdPrefix))
                {
                    continue;
                }
            }

            if (control.HasControls())
            {
                var childControl = FindControlByUniqueId(id, control.Controls);

                if (childControl is not null)
                {
                    return childControl;
                }
            }
        }

        return null;
    }

    private static void FillNamedControls(IDictionary<string, Control> namedControls, ControlCollection collection)
    {
        foreach (var control in collection)
        {
            if (control.ID != null)
            {
                namedControls.TryAdd(control.ID, control);
            }

            if (control is not INamingContainer)
            {
                FillNamedControls(namedControls, control.Controls);
            }
        }
    }

    public virtual void AddParsedSubObject(Control control)
    {
        Controls.AddWithoutPageEvents(control);
    }

    protected virtual void OnFrameworkInit()
    {
        _state = ControlState.FrameworkInitialized;

        FrameworkInitialize();

        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                control.InvokeFrameworkInit();
            }
        }

        FrameworkInitialized();
    }

    /// <summary>Initializes the control that is derived from the <see cref="T:System.Web.UI.TemplateControl" /> class.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void FrameworkInitialize()
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void FrameworkInitialized()
    {
    }

    protected virtual async ValueTask OnPreInitAsync(CancellationToken token)
    {
        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                await control.PreInitAsync(token);
            }
        }
    }

    protected virtual async ValueTask OnInitAsync(CancellationToken token)
    {
        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                await control.InitAsync(token);
            }
        }
    }

    protected virtual void TrackViewState(ViewStateProvider provider)
    {
        _viewState?.TrackViewState();
        _trackViewState = true;
    }

    protected virtual async ValueTask OnLoadAsync(CancellationToken token)
    {
        if (ProcessChildren)
        {
            var form = Page.ActiveForm;

            foreach (var control in Controls)
            {
                if (form != null && control is HtmlForm childForm && childForm.RootForm != form) continue;
                await control.LoadAsync(token);
            }
        }
    }

    protected virtual async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        if (ProcessChildren)
        {
            var form = Page.ActiveForm;

            foreach (var control in Controls)
            {
                if (form != null && control is HtmlForm childForm && childForm.RootForm != form) continue;
                await control.PreRenderAsync(token);
            }
        }
    }

    protected virtual async ValueTask OnUnloadAsync(CancellationToken token)
    {
        if (ProcessChildren)
        {
            foreach (var control in Controls)
            {
                await control.UnloadAsync(token);
            }
        }
    }

    protected virtual void OnWriteViewState(ref ViewStateWriter writer)
    {
        writer.Write<bool?>(_visibleTracking ? _visible : null);

        if (EnableViewStateBag)
        {
            var length = (byte)(_viewState?.ViewStateCount ?? 0);

            writer.Write(length);

            if (length > 0)
            {
                ViewState.Write(ref writer);
            }
        }
    }

    protected virtual void OnLoadViewState(ref ViewStateReader reader)
    {
        if (reader.Read<bool?>() is { } visible)
        {
            _visible = visible;
        }

        if (EnableViewStateBag)
        {
            var length = reader.Read<byte>();

            if (length > 0)
            {
                ViewState.Read(ref reader, length);
            }
        }
    }

    public virtual void StateHasChanged()
    {
        Page.ActiveStreamPanel?.StateHasChanged();
    }

    public void EnsureIdAttribute()
    {
        _forceClientIdAttribute = true;
    }

    public Control LoadControl(string path) => (Control) WebActivator.CreateControl(path);

    public Control LoadControl<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : Control => WebActivator.CreateControl<T>();

    private static readonly string[] AutomaticIDs =
    {
      "c0",
      "c1",
      "c2",
      "c3",
      "c4",
      "c5",
      "c6",
      "c7",
      "c8",
      "c9",
      "c10",
      "c11",
      "c12",
      "c13",
      "c14",
      "c15",
      "c16",
      "c17",
      "c18",
      "c19",
      "c20",
      "c21",
      "c22",
      "c23",
      "c24",
      "c25",
      "c26",
      "c27",
      "c28",
      "c29",
      "c30",
      "c31",
      "c32",
      "c33",
      "c34",
      "c35",
      "c36",
      "c37",
      "c38",
      "c39",
      "c40",
      "c41",
      "c42",
      "c43",
      "c44",
      "c45",
      "c46",
      "c47",
      "c48",
      "c49",
      "c50",
      "c51",
      "c52",
      "c53",
      "c54",
      "c55",
      "c56",
      "c57",
      "c58",
      "c59",
      "c60",
      "c61",
      "c62",
      "c63",
      "c64",
      "c65",
      "c66",
      "c67",
      "c68",
      "c69",
      "c70",
      "c71",
      "c72",
      "c73",
      "c74",
      "c75",
      "c76",
      "c77",
      "c78",
      "c79",
      "c80",
      "c81",
      "c82",
      "c83",
      "c84",
      "c85",
      "c86",
      "c87",
      "c88",
      "c89",
      "c90",
      "c91",
      "c92",
      "c93",
      "c94",
      "c95",
      "c96",
      "c97",
      "c98",
      "c99",
      "c100",
      "c101",
      "c102",
      "c103",
      "c104",
      "c105",
      "c106",
      "c107",
      "c108",
      "c109",
      "c110",
      "c111",
      "c112",
      "c113",
      "c114",
      "c115",
      "c116",
      "c117",
      "c118",
      "c119",
      "c120",
      "c121",
      "c122",
      "c123",
      "c124",
      "c125",
      "c126",
      "c127"
    };

    private static readonly FrozenSet<string> VoidElements;

    static Control()
    {
        var list = new List<string>
        {
            "area",
            "base",
            "br",
            "col",
            "embed",
            "hr",
            "img",
            "input",
            "keygen",
            "link",
            "meta",
            "param",
            "source",
            "track",
            "wbr"
        };

        VoidElements = list.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    protected static bool IsVoidTag(string tagName) =>
        VoidElements.Contains(tagName);

}

internal sealed class OccasionalFields
{
    public string? UniqueIDPrefix;
    public Dictionary<string, Control>? NamedControls;
    public int NextIndex;
}
