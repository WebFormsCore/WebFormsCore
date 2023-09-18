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
