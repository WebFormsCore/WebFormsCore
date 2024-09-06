using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.DisposeTests.Pages;

public sealed class DisposableControl : Control, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
