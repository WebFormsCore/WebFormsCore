using System.Diagnostics;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.BackgroundControl.Pages;

public partial class SlowPage : Page
{
    public const int SlowControlCount = 50;
    public const int ControlDelayMilliseconds = 100;
    public const int MaxElapsedMilliseconds = SlowControlCount * ControlDelayMilliseconds;

    private Stopwatch _stopwatch = null!;

    public int CallCount => Controls.OfType<SlowControl>().Sum(c => c.CallCount);

    public long ElapsedMilliseconds { get; private set; }

    public bool AllowIncremental { get; set; }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        AllowIncremental = !Page.IsPostBack;

        // 100 * 50 = 5000 ms total load time
        for (var i = 0; i < SlowControlCount; i++)
        {
            await Controls.AddAsync(new SlowControl(this));
        }

        _stopwatch = Stopwatch.StartNew();
    }

    protected Task btnPostback_Click(Button sender, EventArgs e)
    {
        // Test if the control is loaded after the event handler
        AllowIncremental = true;
        return Task.CompletedTask;
    }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        await base.OnPreRenderAsync(token);

        ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
    }
}

public class SlowControl(SlowPage slowPage) : Control, IBackgroundLoadHandler
{
    public int CallCount;

    public Task OnBackgroundLoadAsync()
    {
        if (!slowPage.AllowIncremental)
        {
            return Task.CompletedTask; // Skip loading if incremental loading is not allowed
        }

        if (Interlocked.Increment(ref CallCount) > 1)
        {
            throw new Exception("Only one call is allowed.");
        }

        return Task.Delay(SlowPage.ControlDelayMilliseconds); // Simulate a slow load
    }
}