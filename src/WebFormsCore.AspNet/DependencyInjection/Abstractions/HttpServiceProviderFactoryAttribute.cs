using System;

namespace WebFormsCore.Abstractions;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class HttpServiceProviderFactoryAttribute : Attribute
{
    public HttpServiceProviderFactoryAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
