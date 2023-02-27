using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;

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

    Control IControlFactory.CreateControl(IServiceProvider provider)
    {
        return CreateControl(provider);
    }
}
