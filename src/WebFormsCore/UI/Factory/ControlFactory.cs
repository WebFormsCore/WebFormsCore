using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI;

internal sealed class ControlFactory<T> : IControlFactory<T>
{
    private readonly IControlInterceptor<T>[] _interceptors;
    private readonly IControlManager _manager;
    private readonly string[] _viewPaths;
    private readonly bool _noConstructor;

    public ControlFactory(IControlManager manager, IEnumerable<IControlInterceptor<T>> interceptors)
    {
        _manager = manager;
        _interceptors = interceptors.ToArray();

        _viewPaths = typeof(T).GetCustomAttributes<CompiledViewAttribute>().Any()
            ? Array.Empty<string>()
            : typeof(T).GetCustomAttributes<ViewPathAttribute>()
                .Select(i => i.Path)
                .ToArray();

        _noConstructor = _viewPaths.Length == 0 &&
                         typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance).All(i => i.GetParameters().Length == 0);
    }

    public T CreateControl(IServiceProvider provider)
    {
        var control = CreateControlInner(provider);

        foreach (var interceptor in _interceptors)
        {
            control = interceptor.OnControlCreated(control);
        }

        return control;
    }

    private T CreateControlInner(IServiceProvider provider)
    {
        if (_noConstructor)
        {
            return Activator.CreateInstance<T>();
        }

        if (_viewPaths.Length == 0)
        {
            return ActivatorUtilities.CreateInstance<T>(provider);
        }

        if (_viewPaths.Length == 1)
        {
            return (T)ActivatorUtilities.CreateInstance(
                provider,
                _manager.GetType(_viewPaths[0])
            );
        }

        throw new InvalidOperationException(
            $"Controls {typeof(T).FullName} has multiple views. Use <% Register Src %> instead."
        );
    }

    object IControlFactory.CreateControl(IServiceProvider provider)
    {
        return CreateControl(provider)!;
    }
}