using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

internal sealed class ScopedControlContainer : IAsyncDisposable, IDisposable
{
    private readonly HashSet<Control> _controls = new();

    public void Register(Control control)
    {
        _controls.Add(control);
    }

    /// <summary>
    /// Disposes all controls that are not in the page.
    /// </summary>
    public async ValueTask DisposeFloatingControlsAsync()
    {
        foreach (var control in _controls)
        {
            if (!control.IsInPage)
            {
                await DisposeControlAsync(control);
            }
        }

        _controls.RemoveWhere(static i => !i.IsInPage);
    }

    private static ValueTask DisposeControlAsync(Control control)
    {
        if (control is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (control is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return default;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var control in _controls)
        {
            await DisposeControlAsync(control);
        }
    }

    public void Dispose()
    {
        foreach (var control in _controls)
        {
            if (control is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (control is IAsyncDisposable)
            {
                throw new InvalidOperationException("Cannot dispose an IAsyncDisposable control synchronously.");
            }
        }
    }
}
