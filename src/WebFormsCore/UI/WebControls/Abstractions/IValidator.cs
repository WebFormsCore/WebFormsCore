using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public interface IValidator
{
    /// <summary>
    /// Indicates whether the content entered in a control is valid.
    /// </summary>
    bool IsValid { get; set; }

    /// <summary>
    /// Indicates the error message text generated when the control's content is not valid.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// <c>true</c> if the validation control is enabled; otherwise, <c>false</c>.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Compares the entered content with the valid parameters provided by the validation control.
    /// </summary>
    ValueTask ValidateAsync();
}
