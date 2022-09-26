using System;

namespace WebFormsCore.UI;

public interface IControlFactory<out T>
{
    T CreateControl(IServiceProvider provider);
}
