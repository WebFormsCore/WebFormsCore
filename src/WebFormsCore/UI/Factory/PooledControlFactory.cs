using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.ObjectPool;

namespace WebFormsCore.UI;

internal sealed class PooledControlFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : IControlFactory<T>, IDisposable
    where T : Control
{
    private readonly IControlInterceptor<T>[] _interceptors;
    private readonly ObjectPool<T> _pool;
    private readonly List<T> _controls = new();

    public PooledControlFactory(ObjectPool<T> pool, IEnumerable<IControlInterceptor<T>> interceptors)
    {
        _pool = pool;
        _interceptors = interceptors as IControlInterceptor<T>[] ?? interceptors.ToArray();
    }

    public T CreateControl(IServiceProvider provider)
    {
        var control = _pool.Get();
        _controls.Add(control);

        foreach (var interceptor in _interceptors)
        {
            control = interceptor.OnControlCreated(control);
        }

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
