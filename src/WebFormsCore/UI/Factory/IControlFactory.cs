using System;

namespace WebFormsCore.UI;

public interface IControlFactory
{
    Control CreateControl(IServiceProvider provider);
}

public interface IControlFactory<out T> : IControlFactory
    where T : Control
{
    new T CreateControl(IServiceProvider provider);
}
