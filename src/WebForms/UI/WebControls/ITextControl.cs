namespace System.Web.UI.WebControls;

/// <summary>Defines the interface a control implements to get or set its text content.</summary>
public interface ITextControl
{
    /// <summary>Gets or sets the text content of a control.</summary>
    /// <returns>The text content of a control.</returns>
    string Text { get; set; }
}
