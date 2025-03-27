using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.EarlyHints.Pages;

public partial class EarlyHintsPage : Page
{
    private readonly TaskCompletionSource<object?> _scriptLoadedTcs = new();

    public bool? ScriptLoaded { get; private set; }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        var maxWait = Task.Delay(5000, token);
        await Task.WhenAny(maxWait, _scriptLoadedTcs.Task);

        ScriptLoaded ??= false;
    }

    public void MarkScriptLoaded()
    {
        ScriptLoaded ??= true;
        _scriptLoadedTcs.SetResult(null);
    }
}
