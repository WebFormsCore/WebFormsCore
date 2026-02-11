using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.Skeleton;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Represents a single tab within a <see cref="TabControl"/>.
/// Uses <see cref="ITemplate"/> properties for content:
/// <list type="bullet">
///   <item><see cref="TabContent"/> (required) — the panel content</item>
///   <item><see cref="HeaderContent"/> (optional) — custom header; falls back to <see cref="Title"/></item>
/// </list>
/// When <see cref="LazyLoadContent"/> is true, content is wrapped in a
/// <see cref="LazyLoader"/> that performs scoped postbacks, loading only the
/// tab section instead of the full page.
/// </summary>
[ParseChildren(true)]
public partial class Tab : WebControl
{
    private LazyLoader? _lazyLoader;

    public Tab()
        : base(HtmlTextWriterTag.Div)
    {
    }

    /// <summary>
    /// Gets or sets the title displayed in the tab header.
    /// Used as the default header text when <see cref="HeaderContent"/> is null.
    /// </summary>
    [ViewState]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets whether this tab's content should be lazy loaded.
    /// When true, content is wrapped in a <see cref="LazyLoader"/> with
    /// <see cref="LazyLoader.AutoLoad"/> disabled. The tab's client-side
    /// click handler triggers <c>wfc.retriggerLazy</c> on first activation.
    /// </summary>
    [ViewState]
    public bool LazyLoadContent { get; set; }

    /// <summary>
    /// Gets or sets whether clicking this tab triggers a full page postback.
    /// When true, switching to this tab causes a postback so the server-side
    /// <see cref="TabControl.ActiveTabChanged"/> event fires immediately.
    /// </summary>
    [ViewState]
    public bool AutoPostBack { get; set; }

    /// <summary>
    /// Gets or sets the template for the tab panel content.
    /// </summary>
    [TemplateInstance(TemplateInstance.Single)]
    public ITemplate? TabContent { get; set; }

    /// <summary>
    /// Gets or sets an optional template for custom tab header content.
    /// When null, <see cref="Title"/> is used as the header text.
    /// </summary>
    public ITemplate? HeaderContent { get; set; }

    /// <summary>
    /// Event raised when the lazy tab content is loaded.
    /// Only fires for tabs with <see cref="LazyLoadContent"/> set to true.
    /// </summary>
    public event AsyncEventHandler<Tab, EventArgs>? ContentLoaded;

    /// <summary>
    /// Called by <see cref="TabControl"/> to mark this tab as active.
    /// For lazy tabs, marks the inner <see cref="LazyLoader"/> as loaded
    /// so that its children are processed during Load and Render.
    /// </summary>
    internal void SetTabState(bool isActive, TabControl owner)
    {
        if (isActive && _lazyLoader is { IsLoaded: false } && !Page.IsPostBack)
        {
            // Only mark loaded on initial page load (e.g., lazy tab is initially active).
            // During postbacks, the LazyLoader's scoped ViewState may not have been submitted
            // (non-scoped postback skips inputs inside <form data-wfc-form>), so MarkLoaded()
            // would cause it to render content with stale/default values. Instead, the
            // LazyLoader renders a data-wfc-ignore stub and morphdom preserves the client DOM.
            _lazyLoader.MarkLoaded();
        }
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        if (TabContent != null)
        {
            if (LazyLoadContent)
            {
                _lazyLoader = new LazyLoader { AutoLoad = false };
                _lazyLoader.ContentLoaded += OnLazyContentLoaded;
                TabContent.InstantiateIn(_lazyLoader);
                await Controls.AddAsync(_lazyLoader);
            }
            else
            {
                TabContent.InstantiateIn(this);
            }
        }

        await base.OnInitAsync(token);
    }

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);
        writer.AddAttribute("role", "tabpanel");
        writer.AddAttribute("tabindex", "0");
        writer.AddAttribute("aria-labelledby", ClientID + "_tab");
    }

    private Task OnLazyContentLoaded(LazyLoader sender, EventArgs args)
    {
        return ContentLoaded.InvokeAsync(this, EventArgs.Empty).AsTask();
    }

    public override void ClearControl()
    {
        base.ClearControl();

        _lazyLoader = null;
        Title = null;
        LazyLoadContent = false;
        AutoPostBack = false;
        TabContent = null;
        HeaderContent = null;
        ContentLoaded = null;
    }
}
