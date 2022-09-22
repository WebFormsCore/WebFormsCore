namespace System.Web.UI.WebControls;

public enum LiteralMode
{
    /// <summary>The literal control's unsupported markup-language elements are removed. If the literal control is rendered on a browser that supports HTML or XHTML, the control's contents are not modified.</summary>
    Transform,

    /// <summary>The literal control's contents are not modified.</summary>
    PassThrough,

    /// <summary>The literal control's contents are HTML-encoded.</summary>
    Encode,
}