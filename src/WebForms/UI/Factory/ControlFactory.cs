using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

internal sealed class ControlFactory<T> : IControlFactory<T>
{
    private readonly ViewManager _manager;
    private readonly string? _viewPath;

    public ControlFactory(ViewManager manager)
    {
        _manager = manager;
        _viewPath = typeof(T).GetCustomAttribute<ViewPathAttribute>()?.Path;
    }

    public T CreateControl(IServiceProvider provider)
    {
        if (_viewPath == null)
        {
            return ActivatorUtilities.CreateInstance<T>(provider);
        }

        var type = _manager.GetType(_viewPath);
        return (T)ActivatorUtilities.CreateInstance(provider, type);
    }
}
