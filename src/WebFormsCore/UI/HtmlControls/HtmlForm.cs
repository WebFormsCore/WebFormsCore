using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlForm : HtmlContainerControl, INamingContainer, IStateContainer
{
    public HtmlForm()
        : base("form")
    {
    }

    protected override bool ProcessControl => Page._state <= ControlState.Initialized || !Page.IsPostBack || Page.ActiveForm == this;

    public event AsyncEventHandler<HtmlForm, EventArgs>? Submit;

    public bool UpdatePage { get; set; } = true;

    private bool IsDiv => Page.IsExternal;

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

    public override string TagName => IsDiv ? "div" : "form";

    protected internal virtual async Task OnSubmitAsync(CancellationToken token)
    {
        await Submit.InvokeAsync(this, EventArgs.Empty);
    }

    protected override void AfterAddedToParent()
    {
        base.AfterAddedToParent();
        Page.Forms.Add(this);
    }

    protected override void BeforeRemovedFromParent()
    {
        base.BeforeRemovedFromParent();
        Page.Forms.Remove(this);
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);

        if (!IsDiv)
        {
            await writer.WriteAttributeAsync("method", "post");
        }

        await writer.WriteAttributeAsync("id", ClientID);
        await writer.WriteAttributeAsync("data-wfc-form", UpdatePage ? null : "self");
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

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

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!ProcessControl) return default;

        return base.RenderAsync(writer, token);
    }

    public override void ClearControl()
    {
        base.ClearControl();

        UpdatePage = true;
        Submit = null;
    }
}
