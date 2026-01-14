using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI;

public interface IControlFactory
{
    object CreateControl(IServiceProvider provider);
}

public interface IControlFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out T> : IControlFactory
{
    new T CreateControl(IServiceProvider provider);
}

public class FuncControlFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceProvider, T> factory) : IControlFactory<T> where T : notnull
{
    public T CreateControl(IServiceProvider provider) => factory(provider);

    object IControlFactory.CreateControl(IServiceProvider provider) => CreateControl(provider);
}