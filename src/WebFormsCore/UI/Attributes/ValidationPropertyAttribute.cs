using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System.Web.UI;

/// <summary>
/// Identifies the validation property for a component.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidationPropertyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref='ValidationPropertyAttribute'/> class.
    /// </summary>
    public ValidationPropertyAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    ///  Indicates the name the specified validation attribute.
    /// </summary>
    public string Name { get; }
}
