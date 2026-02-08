using System;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Property)]
public class FromRouteAttribute : Attribute
{
    public FromRouteAttribute()
    {
    }

    public FromRouteAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The route parameter name. Defaults to the property name if not specified.
    /// </summary>
    public string? Name { get; }
}
