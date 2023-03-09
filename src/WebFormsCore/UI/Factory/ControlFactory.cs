using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI;

internal sealed class ControlFactory<T> : IControlFactory<T>
    where T : Control
{
    private readonly IControlManager _manager;
    private readonly string[] _viewPaths;

    public ControlFactory(IControlManager manager)
    {
        _manager = manager;
        _viewPaths = typeof(T).GetCustomAttributes<CompiledViewAttribute>().Any()
            ? Array.Empty<string>()
            : typeof(T).GetCustomAttributes<ViewPathAttribute>()
                .Select(i => i.Path)
                .ToArray();
    }

    public T CreateControl(IServiceProvider provider)
    {
        T control;

        if (_viewPaths.Length == 0)
        {
            control = ActivatorUtilities.CreateInstance<T>(provider);
        }
        else if (_viewPaths.Length == 1)
        {
            control = (T)ActivatorUtilities.CreateInstance(
                provider,
                _manager.GetType(_viewPaths[0])
            );
        }
        else
        {
            throw new InvalidOperationException(
                $"Controls {typeof(T).FullName} has multiple views. Use <% Register Src %> instead."
            );
        }

        if (control is IDisposable or IAsyncDisposable)
        {
            provider.GetRequiredService<ScopedControlContainer>().Register(control);
        }

        return control;
    }

    Control IControlFactory.CreateControl(IServiceProvider provider)
    {
        return CreateControl(provider);
    }
}
