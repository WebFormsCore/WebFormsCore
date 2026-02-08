using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System.Web.UI;

using System;

/// <summary>
/// An IDReferencePropertyAttribute metadata attribute can be applied to string properties
/// that contain ID references.
/// This can be used to identify ID reference properties which allows design-time functionality
/// to do interesting things with the property values.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property)]
// ReSharper disable once InconsistentNaming
public sealed class IDReferencePropertyAttribute : Attribute
{
    /// <summary>
    /// </summary>
    public IDReferencePropertyAttribute()
        : this(typeof(Control))
    {
    }


    /// <summary>
    /// Used to mark a property as an ID reference. In addition, the type of controls
    /// can be specified.
    /// </summary>
    public IDReferencePropertyAttribute(Type referencedControlType)
    {
        ReferencedControlType = referencedControlType;
    }

    /// <summary>
    /// The types of controls allowed by the property.
    /// </summary>
    public Type? ReferencedControlType { get; }
}
