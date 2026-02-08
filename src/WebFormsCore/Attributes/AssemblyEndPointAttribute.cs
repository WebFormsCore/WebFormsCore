using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyEndPointAttribute(
    string pattern,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pageType
) : Attribute
{
    public string Pattern { get; } = pattern;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type PageType { get; } = pageType;
}
