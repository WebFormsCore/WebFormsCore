using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton;

/// <summary>
/// A container that renders skeleton placeholders on initial page load, then
/// automatically triggers a postback to load and render real content.
/// Extends <see cref="HtmlForm"/> with <see cref="Scoped"/> set to true so that
/// lazy loaders have their own postback scope and don't block other elements.
/// </summary>
public partial class LazyLoader : HtmlForm, IPostBackAsyncEventHandler
{
    private bool _lazyProcessControl;
    private bool _initComplete;
    private bool _retriggered;

    public LazyLoader()
    {
        Scoped = true;
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
    /// Gets or sets whether the lazy loader auto-triggers a postback on page load.
    /// When false, the <c>data-wfc-lazy</c> attribute renders empty, preventing the
    /// client-side auto-trigger. Content must then be triggered manually via
    /// <c>wfc.retriggerLazy</c> on the client. Default is <c>true</c>.
    /// </summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    /// Resets the lazy loader so that the <see cref="ContentLoaded"/> event fires again
    /// on the next render cycle. The client-side script will detect the non-empty
    /// <c>data-wfc-lazy</c> attribute and automatically trigger a new postback.
    /// This can be called from any control, even outside the lazy loader's form.
    /// </summary>
    public void Retrigger()
    {
        IsLoaded = false;
        _lazyProcessControl = false;
        _retriggered = true;
    }

    /// <summary>
    /// Marks the lazy loader as loaded without triggering a postback.
    /// Used when the content is known to be available (e.g., the container is
    /// already active and should render immediately).
    /// </summary>
    public void MarkLoaded()
    {
        IsLoaded = true;
    }

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
    /// postback (_lazyProcessControl set during OnLoadAsync).
    /// </summary>
    protected override bool ProcessChildren
    {
        get
        {
            if (!_initComplete) return base.ProcessChildren;

            if (IsLoaded || _lazyProcessControl) return base.ProcessChildren;

            return false;
        }
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        // Use UniqueID (not UniqueClientID) because the server checks against UniqueID in OnLoadAsync.
        // When AutoLoad is false, render empty to prevent the client-side auto-trigger.
        await writer.WriteAttributeAsync("data-wfc-lazy", IsLoaded ? "" : (AutoLoad ? UniqueID : ""));

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

        // During a postback that doesn't target this lazy loader and where
        // Retrigger() was not explicitly called, IsLoaded may be false simply
        // because the scoped form's viewstate was not loaded. In that case,
        // render data-wfc-ignore so the client keeps its existing DOM
        // (loaded or skeleton) and doesn't trigger a redundant lazy-load.
        if (Page.IsPostBack && !_lazyProcessControl && !_retriggered && !IsLoaded)
        {
            await writer.WriteBeginTagAsync(TagName);
            await writer.WriteAttributeAsync("id", ClientID);
            await writer.WriteAttributeAsync("data-wfc-form", "self");
            await writer.WriteAttributeAsync("data-wfc-ignore", null);
            await writer.WriteAsync('>');
            await writer.WriteEndTagAsync(TagName);
            return;
        }

        await RenderBeginTagAsync(writer, token);
        await RenderChildrenAsync(writer, token);
        await RenderEndTagAsync(writer, token);
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (IsLoaded)
        {
            // Render actual children and viewstate fields (via base.RenderChildrenAsync which is HtmlForm's)
            await base.RenderChildrenAsync(writer, token);
        }
        else
        {
            // Render skeleton content first
            await RenderSkeletonContentAsync(writer, token);

            // Render the viewstate hidden fields so the scoped form can be identified
            await RenderViewStateFieldsAsync(writer);
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
                _lazyProcessControl = true;
            }
        }

        await base.OnLoadAsync(token);

        if (_lazyProcessControl && !IsLoaded)
        {
            IsLoaded = true;
            await ContentLoaded.InvokeAsync(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Handles a client-side retrigger (<c>wfc.retriggerLazy</c>): the JS function sends
    /// a <c>LAZY_LOAD</c> postback even when <see cref="IsLoaded"/> is still <c>true</c>
    /// in ViewState. Since children were already processed during Load, we just re-fire
    /// <see cref="ContentLoaded"/>. The initial lazy-load case is handled in
    /// <see cref="OnLoadAsync"/> (which sets <c>_lazyProcessControl</c>) and is skipped here.
    /// </summary>
    protected virtual async Task RaisePostBackEventAsync(string? eventArgument)
    {
        if (eventArgument == "LAZY_LOAD" && !_lazyProcessControl)
        {
            await ContentLoaded.InvokeAsync(this, EventArgs.Empty);
        }
    }

    Task IPostBackAsyncEventHandler.RaisePostBackEventAsync(string? eventArgument)
        => RaisePostBackEventAsync(eventArgument);

    public override void ClearControl()
    {
        base.ClearControl();

        // Re-enable Scoped since base.ClearControl() resets it
        Scoped = true;
        IsLoaded = false;
        AutoLoad = true;
        _lazyProcessControl = false;
        _initComplete = false;
        _retriggered = false;
        ContentLoaded = null;
        LoadingCssClass = null;
    }
}
