using System;
using System.Reflection;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class DesignerOptionsAttribute : Attribute
{
    public FieldVisibility Visibility { get; set; }
}

public enum FieldVisibility
{
    Protected,
    Public,
    Internal
}