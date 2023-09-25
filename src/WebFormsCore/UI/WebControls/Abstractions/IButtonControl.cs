namespace WebFormsCore.UI.WebControls;

public interface IButtonControl : ICausesValidationControl
{
    /// <devdoc>
    /// Gets or sets an optional argument that is propogated in
    /// the command event with the associated CommandName
    /// property.
    /// </devdoc>
    string? CommandArgument { get; set; }

    /// <devdoc>
    /// Gets or sets the command associated with the button control that is propogated
    /// in the command event along with the CommandArgument property.
    /// </devdoc>
    string? CommandName { get; set; }

    /// <devdoc>
    /// Represents the method that will handle the Click event of a button control.
    /// </devdoc>
    event AsyncEventHandler Click;

    /// <devdoc>
    /// The text for the button.
    /// </devdoc>
    string? Text { get; set; }
}
