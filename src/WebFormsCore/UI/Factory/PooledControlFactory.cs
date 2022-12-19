using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace WebFormsCore.UI;

internal sealed class PooledControlFactory<T> : IControlFactory<T>, IDisposable
    where T : class
{
    private readonly ObjectPool<T> _pool;
    private readonly ConcurrentStack<T> _controls = new();

    public PooledControlFactory(ObjectPool<T> pool)
    {
        _pool = pool;
    }

    public T CreateControl(IServiceProvider provider)
    {
        var control = _pool.Get();
        _controls.Push(control);
        return control;
    }

    public void Dispose()
    {
        foreach (var control in _controls)
        {
            _pool.Return(control);
        }
    }

    object IControlFactory.CreateControl(IServiceProvider provider)
    {
        return CreateControl(provider);
    }
}
