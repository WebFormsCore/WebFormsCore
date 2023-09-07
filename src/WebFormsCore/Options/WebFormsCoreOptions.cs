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
    /// When no class is specified, the will be hidden using the style attribute which is not recommended.
    /// </summary>
    public string? HiddenClass { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether security headers are enabled.
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;
}
