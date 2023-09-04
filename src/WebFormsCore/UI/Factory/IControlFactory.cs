using System;

namespace WebFormsCore.UI;

public interface IControlFactory
{
    object CreateControl(IServiceProvider provider);
}

public interface IControlFactory<out T> : IControlFactory
{
    new T CreateControl(IServiceProvider provider);
}
