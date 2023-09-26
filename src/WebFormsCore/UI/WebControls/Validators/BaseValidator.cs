using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.WebControls;

public abstract partial class BaseValidator : Label, IValidator
{
    private bool _didValidate;

    [ViewState] private bool? _isValid = true;

    public virtual bool IsValid
    {
        get => _isValid ?? true;
        set => _isValid = value;
    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? AssociatedControlID
    {
        get => base.AssociatedControlID;
        set => throw new NotSupportedException("AssociatedControlID is not supported on BaseValidator.");
    }

    [ViewState]
    public bool SetFocusOnError { get; set; }

    [ViewState]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [IDReferenceProperty]
    public string? ControlToValidate { get; set; }

    [ViewState]
    public string? ErrorMessage { get; set; }

    [ViewState]
    public string? ValidationGroup { get; set; }

    public override bool Enabled
    {
        get => base.Enabled;
        set
        {
            base.Enabled = value;

            if (!value)
            {
                _isValid = true;
            }
        }
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Page.Validators.Add(this);
    }

    protected override void OnUnload(EventArgs args)
    {
        base.OnUnload(args);

        Page.Validators.Remove(this);
    }

    public async ValueTask ValidateAsync()
    {
        _didValidate = true;
        IsValid = true;

        if (!Visible || !Enabled) {
            return;
        }

        var validateId = ControlToValidate;

        if (validateId == null)
        {
            return;
        }

        IsValid = await EvaluateIsValidAsync();

        if (!IsValid && SetFocusOnError && IsInPage)
        {
            var c = NamingContainer?.FindControl(validateId);
            if (c != null)
            {
                validateId = c.ClientID;
            }

            Page.SetValidatorInvalidControlFocus(validateId);
        }
    }

    public abstract ValueTask<bool> EvaluateIsValidAsync();

    public override void ClearControl()
    {
        base.ClearControl();

        _didValidate = false;
        IsValid = true;
        ErrorMessage = null;
        ControlToValidate = null;
        ValidationGroup = null;
        SetFocusOnError = false;
    }

    public Control? GetControlToValidate()
    {
        if (ControlToValidate == null)
        {
            return null;
        }

        var control = NamingContainer?.FindControl(ControlToValidate);

        if (control == null)
        {
            return null;
        }

        return control;
    }

    /// <summary>
    /// Gets the validation value of the control named relative to the validator.
    /// </summary>
    protected string? GetControlValidationValue(string? name)
    {
        if (name is null)
        {
            return null;
        }

        // get the control using the relative name
        var c = NamingContainer?.FindControl(name);
        if (c == null)
        {
            return null;
        }

        if (c is IValidateableControl validateableControl)
        {
            return validateableControl.GetValidationValue();
        }

        // get its validation property
        var prop = GetValidationProperty(c);
        if (prop == null)
        {
            return null;
        }

        // get its value as a string
        var value = prop.GetValue(c);

        return value switch
        {
            ListItem item => item.Value,
            null => string.Empty,
            _ => value.ToString()
        };
    }


    /// <devdoc>
    ///    <para>Helper function to get the validation
    ///       property of a control if it exists.</para>
    /// </devdoc>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Controls implement IValidateableControl")]
    public static PropertyDescriptor? GetValidationProperty(object component)
    {
        var valProp = component.GetType().GetCustomAttribute<ValidationPropertyAttribute>();
        if (valProp is { Name: not null })
        {
            var properties = TypeDescriptor.GetProperties(component);

            return properties[valProp.Name];
        }

        return null;
    }

    protected override void OnPreRender(EventArgs args)
    {
        base.OnPreRender(args);

        var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>();

        if (options?.Value.HiddenClass is null)
        {
            Page.Csp.StyleSrc.AddUnsafeInlineHash("display:none;");
        }
    }

    protected override ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>();

        if (IsValid)
        {
            if (options?.Value.HiddenClass is null)
            {
                writer.MergeAttribute("style", "display:none;");
            }
            else
            {
                writer.MergeAttribute("class", options.Value.HiddenClass);
            }
        }

        if (GetControlToValidate() is {} controlValue)
        {
            writer.AddAttribute("data-wfc-validator", controlValue.ClientID);
        }

        if (_didValidate)
        {
            writer.AddAttribute("data-wfc-validated", IsValid ? "true" : "false");
        }

        return base.AddAttributesToRender(writer, token);
    }

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return base.RenderAsync(writer, token);
    }
}
