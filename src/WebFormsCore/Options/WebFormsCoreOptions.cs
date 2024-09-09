using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public class WebFormsCoreOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the use of <see cref="WebFormsCore.UI.WebControls.StreamPanel"/> is allowed.
    /// </summary>
    public bool AllowStreamPanel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether pages can be loaded from external sources.
    /// </summary>
    public bool AllowExternal { get; set; } = true;

    /// <summary>
    /// Gets or sets the default class for hidden elements.
    /// When no class is specified, elements will be hidden using the style attribute.
    /// </summary>
    public string? HiddenClass { get; set; }

    /// <summary>
    /// Gets or sets the default class for disabled elements.
    /// When no class is specified, aspNetDisabled will be used.
    /// </summary>
    public string? DisabledClass { get; set; } = "aspNetDisabled";

    /// <summary>
    /// Gets or sets a value indicating whether security headers are enabled.
    /// </summary>
    /// <remarks>
    /// This sets the following headers: X-Frame-Options, X-Content-Type-Options, Referrer-Policy and Content-Security-Policy (when CSP is enabled).
    /// </remarks>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// <c>true</c> to add the WebFormsCore script to the page; otherwise, <c>false</c>.
    /// </summary>
    public bool AddWebFormsCoreScript { get; set; } = true;

    /// <summary>
    /// <c>true</c> to add the WebFormsCore head script to the page; otherwise, <c>false</c>.
    /// </summary>
    public bool AddWebFormsCoreHeadScript { get; set; } = true;

    /// <summary>
    /// Default positions for &lt;script&gt; tags in <see cref="ClientScriptManager"/>.
    /// </summary>
    public ScriptPosition DefaultScriptPosition { get; set; } = ScriptPosition.BodyEnd;

    /// <summary>
    /// Default positions for &lt;style&gt; and &lt;link&gt; tags in <see cref="ClientScriptManager"/>.
    /// </summary>
    public ScriptPosition DefaultStylePosition { get; set; } = ScriptPosition.HeadEnd;

    /// <summary>
    /// <c>true</c> to enable the WebForms polyfill; otherwise, <c>false</c>.
    /// </summary>
    public bool EnableWebFormsPolyfill { get; set; } = true;

    /// <summary>
    /// Gets or sets the default literal mode.
    /// </summary>
    public LiteralMode DefaultLiteralMode { get; set; } = LiteralMode.Encode;

    /// <summary>
    /// Gets or sets a value indicating whether the script should be rendered on postback.
    /// </summary>
    public bool RenderScriptOnPostBack { get; set; }
}
