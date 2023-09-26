namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Represents a control that causes validation before firing an event.
/// </summary>
public interface ICausesValidationControl
{
    /// <summary>
    /// Gets or sets whether the control causes validation before firing an event.
    /// </summary>
    bool CausesValidation { get; set; }

    /// <devdoc>
    /// The name of the validation group for the button.
    /// </devdoc>
    string ValidationGroup { get; set; }
}
