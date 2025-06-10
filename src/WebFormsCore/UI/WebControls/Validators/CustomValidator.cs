using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public class ServerValidateEventArgs(string value, bool isValid) : EventArgs
{
    /// <summary>Gets the value of the input control to validate.</summary>
    public string Value { get; } = value;

    /// <summary>Gets or sets whether the input is valid.</summary>
    public bool IsValid { get; set; } = isValid;
}

public partial class CustomValidator : BaseValidator
{
    public event AsyncEventHandler<CustomValidator, ServerValidateEventArgs>? ServerValidate;

    [ViewState] public bool ValidateEmptyText { get; set; } = false;

    public override async ValueTask<bool> EvaluateIsValidAsync()
    {
        if (ServerValidate is null)
        {
            return true;
        }

        var controlValue = GetControlValidationValue(ControlToValidate);

        if (!ValidateEmptyText && string.IsNullOrEmpty(controlValue))
        {
            return true;
        }

        var args = new ServerValidateEventArgs(controlValue ?? string.Empty, true);

        await ServerValidate.Invoke(this, args);

        return args.IsValid;
    }

    protected override ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        writer.AddAttribute("data-wfc-customvalidator", null);

        return base.AddAttributesToRender(writer, token);
    }

    public override void ClearControl()
    {
        base.ClearControl();

        ValidateEmptyText = false;
        ServerValidate = null;
    }
}
