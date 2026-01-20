using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlForm : HtmlContainerControl, INamingContainer, IStateContainer
{
    private bool _processControl;

    public HtmlForm()
        : base("form")
    {
    }

    /// <summary>
    /// Gets the parent form if this form is nested inside another form.
    /// Returns null if this is a root form.
    /// </summary>
    internal HtmlForm? ParentForm => base.Form;

    /// <summary>
    /// Gets a value indicating whether this form is a root form (not nested inside another form).
    /// Only root forms render the wfcForm hidden field and can be the ActiveForm.
    /// </summary>
    internal bool IsRootForm => ParentForm == null;

    protected override bool ProcessControl => ProcessAndRenderControl || _state <= ControlState.Initialized;

    /// <summary>
    /// Gets the root form for this form. If this form is nested, returns the outermost parent form.
    /// If this form is already a root form, returns this form.
    /// </summary>
    internal HtmlForm RootForm => ParentForm?.RootForm ?? this;

    private bool ProcessAndRenderControl => _processControl || !Page.IsPostBack || Page.ActiveForm == RootForm;

    public event AsyncEventHandler<HtmlForm, EventArgs>? Submit;

    public string? LastViewState { get; set; }

    public string? Target
    {
        get => Attributes["target"];
        set => Attributes["target"] = value;
    }

    public string? Action
    {
        get => Attributes["action"];
        set => Attributes["action"] = value;
    }

    public string? Method
    {
        get => Attributes["method"];
        set => Attributes["method"] = value;
    }

    public string? Enctype
    {
        get => Attributes["enctype"];
        set => Attributes["enctype"] = value;
    }

    private bool ShouldRenderAsDiv => Page.IsExternal || !IsRootForm;

    public override string TagName => ShouldRenderAsDiv ? "div" : "form";

    protected override void OnFrameworkInit()
    {
        base.OnFrameworkInit();

        // Dynamically added form
        if (Page._state > ControlState.Initialized)
        {
            _processControl = true;
        }
    }

    protected internal virtual async Task OnSubmitAsync(CancellationToken token)
    {
        await Submit.InvokeAsync(this, EventArgs.Empty);
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Page.Forms.Add(this);
    }

    protected override async ValueTask OnUnloadAsync(CancellationToken token)
    {
        await base.OnUnloadAsync(token);

        Page.Forms.Remove(this);
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);

        if (!ShouldRenderAsDiv)
        {
            await writer.WriteAttributeAsync("method", "post");
        }

        await writer.WriteAttributeAsync("id", ClientID);
        await writer.WriteAttributeAsync("data-wfc-form", null);
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        // Only render the wfcForm hidden field for root forms.
        // Nested forms are processed as part of their root form's postback.
        if (IsRootForm)
        {
            await writer.WriteAsync(@"<input type=""hidden"" name=""wfcForm"" value=""");
            await writer.WriteAsync(UniqueID);
            await writer.WriteAsync(@"""/>");
            await writer.WriteLineAsync();

            if (viewStateManager.EnableViewState && !Page.IsStreaming)
            {
                await writer.WriteAsync(@"<input type=""hidden"" name=""wfcFormState"" value=""");
                using (var viewState = await viewStateManager.WriteAsync(this, out var length))
                {
                    await writer.WriteAsync(viewState.Memory.Slice(0, length));
                }

                await writer.WriteAsync(@"""/>");
            }
        }
    }



    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!ProcessAndRenderControl)
        {
            // Ignore the current form
            await writer.WriteBeginTagAsync(TagName);
            await writer.WriteAttributeAsync("id", ClientID);
            await writer.WriteAttributeAsync("data-wfc-ignore", null);
            await writer.WriteAsync('>');
            await RenderEndTagAsync(writer, token);
            return;
        }

        await base.RenderAsync(writer, token);
    }

    public override void ClearControl()
    {
        base.ClearControl();

        Submit = null;
        LastViewState = null;
        _processControl = false;
    }
}
