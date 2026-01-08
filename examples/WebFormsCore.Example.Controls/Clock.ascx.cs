using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.Example.Controls;

public partial class Clock : Control, IDisposable
{
    private Timer? _timer;

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        if (Page.IsStreaming)
        {
            _timer = new Timer(Update, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        else
        {
            Update(this);
        }
    }

    private static void Update(object? state)
    {
        var clock = Unsafe.As<Clock>(state!);
        clock.litTime.Text = DateTime.Now.ToString("HH:mm:ss");
        clock.StateHasChanged();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}






