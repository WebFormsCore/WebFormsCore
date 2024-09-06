using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Assembly)]
public class RootNamespaceAttribute(string ns) : Attribute
{
    public string Namespace { get; } = ns;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyViewAttribute(
    string path,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type
) : Attribute
{
    public string Path { get; } = path;

    public Type Type { get; } = type;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyControlAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type
) : Attribute
{
    public Type Type { get; } = type;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyControlTypeProviderAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type
) : Attribute
{
    public Type Type { get; } = type;
}
