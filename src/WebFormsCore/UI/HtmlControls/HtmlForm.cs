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

    public override bool EnableViewState { get; set; } = true;

    private bool IsDiv => Page.IsExternal;

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

    protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);

        if (!IsDiv)
        {
            await writer.WriteAttributeAsync("method", "post");
        }

        await writer.WriteAttributeAsync("id", ClientID);
        await writer.WriteAttributeAsync("data-wfc-form", null);
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        await base.RenderChildrenAsync(writer, token);

        await writer.WriteAsync(@"<input type=""hidden"" name=""wfcForm"" value=""");
        await writer.WriteAsync(UniqueID);
        await writer.WriteAsync(@"""/>");

        if (viewStateManager.EnableViewState)
        {
            await writer.WriteAsync(@"<input type=""hidden"" name=""wfcFormState"" value=""");
            using (var viewState = viewStateManager.WriteBase64(this, out var length))
            {
                await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
            }

            await writer.WriteAsync(@"""/>");
        }
    }

    public override Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!ProcessControl) return Task.CompletedTask;

        return base.RenderAsync(writer, token);
    }

    protected override string GetUniqueIDPrefix() => "";
}
