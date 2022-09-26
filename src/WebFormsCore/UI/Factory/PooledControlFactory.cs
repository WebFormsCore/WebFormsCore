using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace WebFormsCore.UI;

internal sealed class PooledControlFactory<T> : IControlFactory<T>, IDisposable
    where T : class
{
    private readonly ObjectPool<T> _pool;
    private readonly List<T> _controls = new();

    public PooledControlFactory(ObjectPool<T> pool)
    {
        _pool = pool;
    }

    public T CreateControl(IServiceProvider provider)
    {
        var control = _pool.Get();
        _controls.Add(control);
        return control;
    }

    public void Dispose()
    {
        foreach (var control in _controls)
        {
            _pool.Return(control);
        }
    }
}
