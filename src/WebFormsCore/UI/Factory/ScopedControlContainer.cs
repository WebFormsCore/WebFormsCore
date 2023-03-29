using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

internal sealed class ScopedControlContainer : IAsyncDisposable, IDisposable
{
    private readonly HashSet<object> _controls = new();

    public void Register(object control)
    {
        _controls.Add(control);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var control in _controls)
        {
            if (control is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (control is IDisposable disposable)
            {
                disposable.Dispose();
            }
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
