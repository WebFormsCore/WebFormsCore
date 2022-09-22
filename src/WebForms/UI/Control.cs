using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;
using HttpContext = System.Web.HttpContext;

namespace WebFormsCore.UI;

public partial class Control
{
    protected const char IdSeparator = '$';

    private StateBag? _viewState;
    private ControlCollection? _controls;
    private Control? _namingContainer;
    private string? _id;
    private string? _cachedUniqueID;
    private string? _cachedPredictableID;
    private Control? _parent;
    private OccasionalFields? _occasionalFields;
    private Page? _page;
    private HtmlForm? _form;
    private IWebObjectActivator? _webObjectActivator;

    protected IWebObjectActivator WebActivator => _webObjectActivator ??= ServiceProvider.GetRequiredService<IWebObjectActivator>();

    protected virtual IServiceProvider ServiceProvider => Page.ServiceProvider;

    public bool EnableViewState { get; set; } = true;

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
            if (_page == null && _parent != null)
            {
                if (_parent is Page page)
                {
                    _page = page;
                }
                else
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
                if (_parent is HtmlForm page)
                {
                    _form = page;
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
            _id = value;

            ClearCachedUniqueIDRecursive();

            if (_namingContainer != null && id != null)
            {
                _namingContainer.DirtyNameTable();
            }

            if (id != null && id != _id)
            {
                ClearCachedClientID();
            }
        }
    }

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
            if (namingContainer == null) return _id;

            _id ??= GenerateAutomaticId();

            if (Page == namingContainer)
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

    public ClientIDMode EffectiveClientIDModeValue { get; set; }

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
            return EffectiveClientIDMode switch
            {
                ClientIDMode.Predictable => PredictableClientID,
                ClientIDMode.Static => ID,
                _ => UniqueClientID
            };
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

    internal string PredictableClientID => (_cachedPredictableID ??= GetPredictableClientIDPrefix()) ?? "";

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
    protected virtual HttpContext Context => _page?.Context ?? HttpContext.Current ?? throw new InvalidOperationException("HttpContext is not available");

    public virtual void ClearControl()
    {
        EffectiveClientIDModeValue = ClientIDMode.Inherit;
        ClientIDModeValue = ClientIDMode.Inherit;
        _viewState = null;
        _controls = null;
        _namingContainer = null;
        _id = null;
        _cachedUniqueID = null;
        _cachedPredictableID = null;
        _parent = null;
        _occasionalFields = null;
        _page = null;
        _form = null;
    }

    private string GetUniqueIDPrefix()
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
    public virtual ControlCollection Controls => _controls ??= CreateControlCollection();

    /// <summary>Gets a dictionary of state information that allows you to save and restore the view state of a server control across multiple requests for the same page.</summary>
    /// <returns>An instance of the <see cref="T:WebFormsCore.UI.StateBag" /> class that contains the server control's view-state information.</returns>
    protected virtual StateBag ViewState
    {
        get
        {
            if (_viewState != null) return _viewState;
            _viewState = new StateBag(ViewStateIgnoresCase);
            return _viewState;
        }
    }

    private OccasionalFields OccasionalFields => _occasionalFields ??= new OccasionalFields();

    /// <summary>Gets a value that indicates whether the <see cref="T:WebFormsCore.UI.StateBag" /> object is case-insensitive.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:WebFormsCore.UI.StateBag" /> instance is case-insensitive; otherwise, <see langword="false" />. The default is <see langword="false" />.</returns>
    protected virtual bool ViewStateIgnoresCase => false;
    
    /// <summary>Sends server control content to a provided <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object, which writes the content to be rendered on the client.</summary>
    /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object that receives the server control content. </param>
    public virtual ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return RenderChildrenAsync(writer, token);
    }

    /// <summary>Outputs the content of a server control's children to a provided <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object, which writes the content to be rendered on the client.</summary>
    /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object that receives the rendered content. </param>
    protected virtual async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (_controls == null || _controls.Count == 0 || token.IsCancellationRequested) return;

        foreach (var control in _controls)
        {
            await control.RenderAsync(writer, token);
        }
    }

    /// <summary>Creates a new <see cref="T:WebFormsCore.UI.ControlCollection" /> object to hold the child controls (both literal and server) of the server control.</summary>
    /// <returns>A <see cref="T:WebFormsCore.UI.ControlCollection" /> object to contain the current server control's child server controls.</returns>
    protected virtual ControlCollection CreateControlCollection()
    {
        return new ControlCollection(this);
    }

    internal void AddedControl(Control control, int index)
    {
        control._parent?.Controls.Remove(control);
        control._parent = this;
        control._page = _page;
        control._form = _form;

        if (NamingContainer is not { } namingContainer) return;

        control.UpdateNamingContainer(namingContainer);

        if (control._id == null)
        {
            control.GenerateAutomaticId();
        }
        else if (control._id != null || control._controls != null)
        {
            namingContainer.DirtyNameTable();
        }
    }

    internal void ClearNamingContainer()
    {
        OccasionalFields.NamedControlsID = 0;
        DirtyNameTable();
    }

    internal void RemovedControl(Control control)
    {
        control._parent = null;
        control._page = null;
        control._form = null;
        control._namingContainer = null;
    }


    private void UpdateNamingContainer(Control namingContainer)
    {
        if (_namingContainer == null || _namingContainer != null && _namingContainer != namingContainer)
        {
            ClearCachedUniqueIDRecursive();
        }

        if (EffectiveClientIDModeValue != ClientIDMode.Inherit)
        {
            ClearCachedClientID();
            ClearEffectiveClientIDMode();
        }

        _namingContainer = namingContainer;
    }

    private string? GetPredictableClientIDPrefix()
    {
        var namingContainer = NamingContainer;
        string? predictableClientIdPrefix;

        if (namingContainer != null)
        {
            _id ??= GenerateAutomaticId();

            switch (namingContainer)
            {
                case Page _:
                case MasterPage _:
                    predictableClientIdPrefix = _id;
                    break;
                default:
                    predictableClientIdPrefix = ClientID;

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

    private string GenerateAutomaticId()
    {
        if (_namingContainer == null)
        {
            return AutomaticIDs[0];
        }

        var index = _namingContainer.OccasionalFields.NamedControlsID++;
        _id = index >= 128 ? "ctl" + index.ToString(NumberFormatInfo.InvariantInfo) : AutomaticIDs[index];
        _namingContainer.DirtyNameTable();
        return _id;
    }

    private void ClearCachedUniqueIDRecursive()
    {
        _cachedUniqueID = null;
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



    /// <summary>Searches the current naming container for a server control with the specified <paramref name="id" /> parameter.</summary>
    /// <param name="id">The identifier for the control to be found. </param>
    /// <returns>The specified control, or <see langword="null" /> if the specified control does not exist.</returns>
    public virtual Control? FindControl(string id)
    {
        if (this is not INamingContainer)
        {
            return NamingContainer?.FindControl(id);
        }

        if (Controls.Count == 0)
        {
            return null;
        }

        if (OccasionalFields.NamedControls is not { } namedControls)
        {
            namedControls = new Dictionary<string, Control>();
            FillNamedControls(namedControls, Controls);
            OccasionalFields.NamedControls = namedControls;
        }

        return namedControls.TryGetValue(id, out var control) ? control : null;
    }

    private static void FillNamedControls(IDictionary<string, Control> namedControls, ControlCollection collection)
    {
        foreach (var control in collection)
        {
            if (control._id != null)
            {
                namedControls.Add(control._id, control);
            }

            FillNamedControls(namedControls, control.Controls);
        }
    }

    public virtual void AddParsedSubObject(Control control)
    {
        Controls.Add(control);
    }

    protected virtual void OnInit(EventArgs args)
    {
    }
    
    protected virtual ValueTask OnInitAsync(CancellationToken token)
    {
        return default;
    }

    protected virtual void OnLoad(EventArgs args)
    {
    }

    protected virtual ValueTask OnLoadAsync(CancellationToken token)
    {
        return default;
    }

    protected virtual void OnWriteViewState(ref ViewStateWriter writer)
    {
        if (!EnableViewState) return;
        
        var length = (byte)(_viewState?.ViewStateCount ?? 0);

        writer.Write(length);

        if (length > 0)
        {
            ViewState.Write(ref writer);
        }
    }

    protected virtual void OnReadViewState(ref ViewStateReader reader)
    {
        if (!EnableViewState) return;

        var length = reader.Read<byte>();

        if (length > 0)
        {
            ViewState.Read(ref reader, length);
        }
    }

    /// <summary>Initializes the control that is derived from the <see cref="T:System.Web.UI.TemplateControl" /> class.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void FrameworkInitialize()
    {
    }

    private static readonly string[] AutomaticIDs =
    {
      "ctl00",
      "ctl01",
      "ctl02",
      "ctl03",
      "ctl04",
      "ctl05",
      "ctl06",
      "ctl07",
      "ctl08",
      "ctl09",
      "ctl10",
      "ctl11",
      "ctl12",
      "ctl13",
      "ctl14",
      "ctl15",
      "ctl16",
      "ctl17",
      "ctl18",
      "ctl19",
      "ctl20",
      "ctl21",
      "ctl22",
      "ctl23",
      "ctl24",
      "ctl25",
      "ctl26",
      "ctl27",
      "ctl28",
      "ctl29",
      "ctl30",
      "ctl31",
      "ctl32",
      "ctl33",
      "ctl34",
      "ctl35",
      "ctl36",
      "ctl37",
      "ctl38",
      "ctl39",
      "ctl40",
      "ctl41",
      "ctl42",
      "ctl43",
      "ctl44",
      "ctl45",
      "ctl46",
      "ctl47",
      "ctl48",
      "ctl49",
      "ctl50",
      "ctl51",
      "ctl52",
      "ctl53",
      "ctl54",
      "ctl55",
      "ctl56",
      "ctl57",
      "ctl58",
      "ctl59",
      "ctl60",
      "ctl61",
      "ctl62",
      "ctl63",
      "ctl64",
      "ctl65",
      "ctl66",
      "ctl67",
      "ctl68",
      "ctl69",
      "ctl70",
      "ctl71",
      "ctl72",
      "ctl73",
      "ctl74",
      "ctl75",
      "ctl76",
      "ctl77",
      "ctl78",
      "ctl79",
      "ctl80",
      "ctl81",
      "ctl82",
      "ctl83",
      "ctl84",
      "ctl85",
      "ctl86",
      "ctl87",
      "ctl88",
      "ctl89",
      "ctl90",
      "ctl91",
      "ctl92",
      "ctl93",
      "ctl94",
      "ctl95",
      "ctl96",
      "ctl97",
      "ctl98",
      "ctl99",
      "ctl100",
      "ctl101",
      "ctl102",
      "ctl103",
      "ctl104",
      "ctl105",
      "ctl106",
      "ctl107",
      "ctl108",
      "ctl109",
      "ctl110",
      "ctl111",
      "ctl112",
      "ctl113",
      "ctl114",
      "ctl115",
      "ctl116",
      "ctl117",
      "ctl118",
      "ctl119",
      "ctl120",
      "ctl121",
      "ctl122",
      "ctl123",
      "ctl124",
      "ctl125",
      "ctl126",
      "ctl127"
    };
}

internal sealed class OccasionalFields
{
    public int NamedControlsID;
    public string? UniqueIDPrefix;
    public Dictionary<string, Control>? NamedControls;
}
