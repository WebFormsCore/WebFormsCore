using System;
using System.Runtime.CompilerServices;
using System.Threading;
using WebFormsCore.UI;

namespace WebFormsCore.Example.Controls;

public partial class Clock : Control, IDisposable
{
    private Timer? _timer;

    protected override void OnLoad(EventArgs args)
    {
        base.OnLoad(args);

        _timer = new Timer(Update, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
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






