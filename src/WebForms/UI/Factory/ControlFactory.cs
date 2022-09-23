using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI;

internal class ControlFactory<T> : IControlFactory<T>
{
    private readonly PageFactory _factory;
    private readonly string? _viewPath;

    public ControlFactory(PageFactory factory)
    {
        _factory = factory;
        _viewPath = typeof(T).GetCustomAttribute<ViewPathAttribute>()?.Path;
    }

    public T CreateControl(IServiceProvider provider)
    {
        if (_viewPath == null)
        {
            return ActivatorUtilities.CreateInstance<T>(provider);
        }

        var type = _factory.GetType(_viewPath);
        return (T)ActivatorUtilities.CreateInstance(provider, type);
    }
}
