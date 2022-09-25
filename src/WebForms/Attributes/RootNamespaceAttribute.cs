using System;

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
