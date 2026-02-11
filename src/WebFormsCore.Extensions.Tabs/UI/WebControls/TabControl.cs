using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// A tab control that renders a tabbed interface with client-side switching.
/// Each tab is defined as a <see cref="Tab"/> child using <c>&lt;TabContent&gt;</c>
/// templates. Loaded tabs switch client-side (no postback); lazy tabs use a
/// scoped <see cref="WebFormsCore.UI.Skeleton.LazyLoader"/> so only the tab
/// content is loaded on activation, not the entire page.
/// </summary>
/// <example>
/// <code>
/// &lt;wfc:TabControl ID="TabControl1" runat="server"&gt;
///     &lt;Tabs&gt;
///         &lt;wfc:Tab Title="Tab 1"&gt;
///             &lt;TabContent&gt;
///                 &lt;p&gt;Content for Tab 1&lt;/p&gt;
///             &lt;/TabContent&gt;
///         &lt;/wfc:Tab&gt;
///         &lt;wfc:Tab Title="Tab 2" LazyLoadContent="true"&gt;
///             &lt;TabContent&gt;
///                 &lt;p&gt;Content for Tab 2 (loaded on demand)&lt;/p&gt;
///             &lt;/TabContent&gt;
///         &lt;/wfc:Tab&gt;
///     &lt;/Tabs&gt;
/// &lt;/wfc:TabControl&gt;
/// </code>
/// </example>
[ParseChildren(true, "Tabs")]
public partial class TabControl : WebControl, INamingContainer, IPostBackAsyncDataHandler
{
    private readonly List<Tab> _tabs = new();

    public TabControl()
        : base(HtmlTextWriterTag.Div)
    {
    }

    /// <summary>
    /// Gets the collection of <see cref="Tab"/> items in this control.
    /// Populated by the parser from the &lt;Tabs&gt; child element.
    /// </summary>
    public List<Tab> Tabs => _tabs;

    /// <summary>
    /// Gets or sets the zero-based index of the currently active tab
    /// (among visible tabs only).
    /// </summary>
    [ViewState]
    public int ActiveTabIndex { get; set; }

    /// <summary>
    /// Gets or sets the CSS class applied to the tab header list.
    /// </summary>
    public string? HeaderCssClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class applied to the active tab header.
    /// </summary>
    public string? ActiveTabCssClass { get; set; }

    /// <summary>
    /// Gets or sets the accessible label for the tab list.
    /// Rendered as <c>aria-label</c> on the <c>tablist</c> element.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Event raised when the active tab changes.
    /// </summary>
    public event AsyncEventHandler<TabControl, EventArgs>? ActiveTabChanged;

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        Page.ClientScript.RegisterStartupDeferStaticScript(typeof(TabControl), "/js/tabs.min.js", Resources.Script);

        var visibleTabs = GetVisibleTabs();

        for (var i = 0; i < visibleTabs.Count; i++)
        {
            await Controls.AddAsync(visibleTabs[i]);
        }

        await base.OnInitAsync(token);
    }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        // Sync tab states BEFORE base.OnLoadAsync so that the active lazy tab's
        // LazyLoader is marked loaded and its children are processed during Load.
        // ActiveTabIndex may have been updated by LoadPostDataAsync (postback).
        var visibleTabs = GetVisibleTabs();
        var activeIndex = ClampActiveIndex(visibleTabs);

        for (var i = 0; i < visibleTabs.Count; i++)
        {
            visibleTabs[i].SetTabState(i == activeIndex, this);
        }

        await base.OnLoadAsync(token);
    }

    /// <summary>
    /// Reads the active tab index from the form data posted by the client-side
    /// submit hook. The form key is the control's <see cref="Control.UniqueID"/>.
    /// </summary>
    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(
        string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (!IsEnabled || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        var stringValue = value.ToString();

        if (string.IsNullOrEmpty(stringValue) || !int.TryParse(stringValue, out var clientIndex))
        {
            return new ValueTask<bool>(false);
        }

        var visibleTabs = GetVisibleTabs();

        if (clientIndex < 0 || clientIndex >= visibleTabs.Count || !visibleTabs[clientIndex].Enabled)
        {
            return new ValueTask<bool>(false);
        }

        if (clientIndex == ActiveTabIndex)
        {
            return new ValueTask<bool>(false);
        }

        ActiveTabIndex = clientIndex;
        return new ValueTask<bool>(ActiveTabChanged != null);
    }

    /// <summary>
    /// Raises the <see cref="ActiveTabChanged"/> event after postback data processing.
    /// </summary>
    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
    {
        return ActiveTabChanged.InvokeAsync(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the visible tabs (tabs whose Visible property is true).
    /// </summary>
    private List<Tab> GetVisibleTabs()
    {
        var visible = new List<Tab>();
        foreach (var tab in _tabs)
        {
            if (tab.Visible)
                visible.Add(tab);
        }
        return visible;
    }

    private int ClampActiveIndex(List<Tab> visibleTabs)
    {
        if (visibleTabs.Count == 0) return 0;
        return Math.Clamp(ActiveTabIndex, 0, visibleTabs.Count - 1);
    }

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);
        writer.AddAttribute("data-wfc-tabs", null);
        writer.AddAttribute("data-wfc-tab-name", UniqueID);
    }

    protected override async ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var visibleTabs = GetVisibleTabs();

        if (visibleTabs.Count == 0)
            return;

        var activeIndex = ClampActiveIndex(visibleTabs);

        // Render tab header list
        await RenderTabHeadersAsync(writer, visibleTabs, activeIndex, token);

        // Render tab panels
        for (var i = 0; i < visibleTabs.Count; i++)
        {
            var tab = visibleTabs[i];
            var isActive = i == activeIndex;

            if (isActive)
            {
                // Ensure ViewState-persisted inactive styles are cleared
                tab.Style.Remove("display");
                tab.Attributes.Remove("aria-hidden");
            }
            else
            {
                tab.Style.Add("display", "none");
                tab.Attributes["aria-hidden"] = "true";
            }

            await tab.RenderAsync(writer, token);
        }
    }

    private async ValueTask RenderTabHeadersAsync(
        HtmlTextWriter writer,
        List<Tab> visibleTabs,
        int activeIndex,
        CancellationToken token)
    {
        writer.AddAttribute("role", "tablist");
        if (!string.IsNullOrEmpty(AriaLabel))
        {
            writer.AddAttribute("aria-label", AriaLabel);
        }
        if (!string.IsNullOrEmpty(HeaderCssClass))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, HeaderCssClass);
        }
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Ul);

        for (var i = 0; i < visibleTabs.Count; i++)
        {
            var tab = visibleTabs[i];
            var isActive = i == activeIndex;
            var tabId = tab.ClientID;

            // <li role="presentation">
            writer.AddAttribute("role", "presentation");
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Li);

            // <button role="tab">
            writer.AddAttribute("role", "tab");
            writer.AddAttribute("id", tabId + "_tab");
            writer.AddAttribute("aria-selected", isActive ? "true" : "false");
            writer.AddAttribute("aria-controls", tabId);
            writer.AddAttribute("tabindex", isActive ? "0" : "-1");
            writer.AddAttribute("data-wfc-tab-index", i.ToString());

            if (!tab.Enabled)
            {
                writer.AddAttribute("disabled", "disabled");
            }

            if (tab.AutoPostBack)
            {
                writer.AddAttribute("data-wfc-tab-autopostback", null);
            }

            if (!string.IsNullOrEmpty(ActiveTabCssClass) && isActive)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, ActiveTabCssClass);
            }

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Button);

            // Render header content: use HeaderContent template if set, otherwise Title
            if (tab.HeaderContent != null)
            {
                var placeholder = new PlaceHolder();
                tab.HeaderContent.InstantiateIn(placeholder);
                foreach (Control child in placeholder.Controls)
                {
                    await child.RenderAsync(writer, token);
                }
            }
            else
            {
                await writer.WriteAsync(tab.Title ?? string.Empty);
            }

            await writer.RenderEndTagAsync(); // </button>

            await writer.RenderEndTagAsync(); // </li>
        }

        await writer.RenderEndTagAsync(); // </ul>
    }

    public override void ClearControl()
    {
        base.ClearControl();

        _tabs.Clear();
        ActiveTabIndex = 0;
        HeaderCssClass = null;
        ActiveTabCssClass = null;
        ActiveTabChanged = null;
        AriaLabel = null;
    }
}
