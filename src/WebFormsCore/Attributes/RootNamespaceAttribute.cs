using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Assembly)]
public class RootNamespaceAttribute : Attribute
{
    public RootNamespaceAttribute(string ns)
    {
        Namespace = ns;
    }

    public string Namespace { get; }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyViewAttribute : Attribute
{
public AssemblyViewAttribute(string path, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
{
    Path = path;
    Type = type;
}

    public string Path { get; }

    public Type Type { get; }
}


[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyControlAttribute : Attribute
{
    public AssemblyControlAttribute(
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
