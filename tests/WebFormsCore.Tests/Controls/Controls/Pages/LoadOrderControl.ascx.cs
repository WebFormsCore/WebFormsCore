using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Pages;

public partial class LoadOrderControl : Control
{
    public string Text
    {
        get => lbl.Text;
        set => lbl.Text = value;
    }

    protected override void OnFrameworkInit()
    {
        AssertChildState(ControlState.Constructed, "FrameworkInitialize (before base)");
        base.OnFrameworkInit();
        AssertChildState(ControlState.FrameworkInitialized, "FrameworkInitialize (after base)");
    }

    protected override void FrameworkInitialize()
    {
        AssertChildState(ControlState.Constructed, "FrameworkInitialize (before base)");
        base.FrameworkInitialize();
    }

    protected override async ValueTask OnPreInitAsync(CancellationToken token)
    {
        AssertChildState(ControlState.FrameworkInitialized, "OnPreInitAsync (before base)");
        await base.OnPreInitAsync(token);
        AssertChildState(ControlState.PreInitialized, "OnPreInitAsync (after base)");
    }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        AssertChildState(ControlState.PreInitialized, "OnInitAsync (before base)");
        await base.OnInitAsync(token);
        AssertChildState(ControlState.Initialized, "OnInitAsync (after base)");
    }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        AssertChildState(ControlState.Initialized, "OnLoadAsync (before base)");
        await base.OnLoadAsync(token);
        AssertChildState(ControlState.Loaded, "OnLoadAsync (after base)");
    }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        AssertChildState(ControlState.Loaded, "OnPreRenderAsync (before base)");
        await base.OnPreRenderAsync(token);
        AssertChildState(ControlState.PreRendered, "OnPreRenderAsync (after base)");
    }

    private void AssertChildState(ControlState expectedState, string phase)
    {
        foreach (var child in this.EnumerateSelfAndChildControls())
        {
            if (child == this) continue;

            if (child._state != expectedState)
            {
                throw new InvalidOperationException($"Child control '{child.ID ?? child.GetType().Name}' has state {child._state}, expected {expectedState} during {phase}");
            }
        }
    }
}