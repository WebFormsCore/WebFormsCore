using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class RequiredFieldValidator : BaseValidator
{
    [ViewState] public string InitialValue { get; set; } = "";

    public override ValueTask<bool> EvaluateIsValidAsync()
    {
        var controlValue = GetControlValidationValue(ControlToValidate);

        if (controlValue == null)
        {
            return new ValueTask<bool>(true);
        }

        var controlValueSpan = controlValue.AsSpan().Trim();
        var initialValueSpan = InitialValue.AsSpan().Trim();

        return new ValueTask<bool>(!controlValueSpan.Equals(initialValueSpan, StringComparison.CurrentCulture));
    }

    protected override void OnPreRender(EventArgs args)
    {
        base.OnPreRender(args);

        Page.ClientScript.RegisterStartupScript(GetType(), "RequiredFieldValidator", """
            WebFormsCore.bindValidator('[data-wfc-requiredvalidator]', function(elementToValidate, element) {
                const initialValue = (element.getAttribute('data-wfc-requiredvalidator') ?? "").trim();
                const value = (elementToValidate.value ?? "").trim();
            
                return initialValue !== value
            });
            """);
    }

    protected override ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        writer.AddAttribute("data-wfc-requiredvalidator", InitialValue);

        return base.AddAttributesToRender(writer, token);
    }

    public override void ClearControl()
    {
        base.ClearControl();

        InitialValue = "";
    }
}
