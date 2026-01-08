using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Security;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Tests.EarlyHints.Pages;

public partial class EarlyHintsPage : Page
{
    private readonly TaskCompletionSource<object?> _scriptLoadedTcs = new();

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        // The script is not in the page, we only want to hint it to validate the test
        EarlyHints.AddScript("/script.js");
    }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        await Task.WhenAny(Task.Delay(5000, token), _scriptLoadedTcs.Task);
    }

    public void MarkScriptLoaded()
    {
        _scriptLoadedTcs.SetResult(null);
    }
}
