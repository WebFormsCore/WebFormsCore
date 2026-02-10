using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton;

/// <summary>
/// A container that renders skeleton placeholders on initial page load, then
/// automatically triggers a postback to load and render real content.
/// Implements <see cref="INamingContainer"/> and <see cref="IStateContainer"/>
/// for ViewState scoping.
/// </summary>
public partial class LazyLoader : HtmlContainerControl, INamingContainer, IStateContainer
{
    private bool _processControl;
    private bool _initComplete;

    public LazyLoader()
        : base("div")
    {
    }

    protected override bool AddClientIdToAttributes => true;

    /// <summary>
    /// Event raised when the lazy content loads (on the async postback).
    /// </summary>
    public event AsyncEventHandler<LazyLoader, EventArgs>? ContentLoaded;

    /// <summary>
    /// Optional CSS class applied to the container during skeleton/loading state.
    /// </summary>
    public string? LoadingCssClass { get; set; }

    /// <summary>
    /// Gets whether the lazy loader has loaded its content.
    /// Persisted in ViewState so that subsequent postbacks know content is already loaded.
    /// </summary>
    [ViewState]
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// The LazyLoader container itself always participates in the control lifecycle
    /// and ViewState. This ensures consistent control counts between ViewState save
    /// and load. Child processing is controlled separately via <see cref="ProcessChildren"/>.
    /// </summary>
    protected override bool ProcessControl => true;

    /// <summary>
    /// Controls whether children are processed during lifecycle phases.
    /// Before Init completes, children are always processed so the control tree
    /// gets built and ViewState tracking is set up.
    /// After Init, children are only processed when the lazy content has loaded
    /// (IsLoaded restored from ViewState) or when this is the targeted lazy-load
    /// postback (_processControl set during OnLoadAsync).
    /// </summary>
    protected override bool ProcessChildren
    {
        get
        {
            if (!_initComplete) return base.ProcessChildren;

            if (IsLoaded || _processControl) return base.ProcessChildren;

            return false;
        }
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        await writer.WriteAttributeAsync("data-wfc-lazy", IsLoaded ? "" : UniqueClientID);

        if (!IsLoaded)
        {
            await writer.WriteAttributeAsync("aria-busy", "true");

            if (!string.IsNullOrEmpty(LoadingCssClass))
            {
                var existingClass = Attributes["class"];
                var fullClass = string.IsNullOrEmpty(existingClass)
                    ? LoadingCssClass
                    : existingClass + " " + LoadingCssClass;
                await writer.WriteAttributeAsync("class", fullClass);
            }
        }
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Visible) return;

        if (IsLoaded)
        {
            // Render actual content
            await base.RenderAsync(writer, token);
        }
        else
        {
            // Render skeleton version
            await RenderBeginTagAsync(writer, token);
            await RenderSkeletonContentAsync(writer, token);
            await RenderEndTagAsync(writer, token);
        }
    }

    private async ValueTask RenderSkeletonContentAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (Controls.Count == 0)
        {
            await RenderFallbackSkeletonAsync(writer);
            return;
        }

        await SkeletonContainer.RenderChildSkeletonsAsync(writer, Controls, ServiceProvider, token);
    }

    private static async ValueTask RenderFallbackSkeletonAsync(HtmlTextWriter writer)
    {
        writer.AddAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await writer.WriteAsync("&nbsp;");
        await writer.RenderEndTagAsync();
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);
        _initComplete = true;
    }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        // Detect if this is a lazy-load postback targeting this control.
        // This must be set BEFORE base.OnLoadAsync so that ProcessChildren
        // returns true and children go through Load.
        if (Page.IsPostBack && !IsLoaded)
        {
            var target = Request.Form["wfcTarget"].ToString();

            if (!string.IsNullOrEmpty(target) &&
                (target == UniqueID || target.StartsWith(GetUniqueIDPrefix())))
            {
                _processControl = true;
            }
        }

        await base.OnLoadAsync(token);

        if (_processControl && !IsLoaded)
        {
            IsLoaded = true;
            await ContentLoaded.InvokeAsync(this, EventArgs.Empty);
        }
    }

    public override void ClearControl()
    {
        base.ClearControl();

        IsLoaded = false;
        _processControl = false;
        _initComplete = false;
        ContentLoaded = null;
        LoadingCssClass = null;
    }
}
