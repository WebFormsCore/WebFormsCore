using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System.Web.UI;

/// <summary>Specifies how an ASP.NET server control property or event is persisted declaratively in an .aspx or .ascx file.</summary>
public enum PersistenceMode
{
    /// <summary>Specifies that the property or event persists as an attribute.</summary>
    Attribute,

    /// <summary>Specifies that the property persists in the ASP.NET server control as a nested tag. This is commonly used for complex objects, those that have persistable properties of their own.</summary>
    InnerProperty,

    /// <summary>Specifies that the property persists in the ASP.NET server control as inner text. Also indicates that this property is defined as the element's default property. Only one property can be designated the default property.</summary>
    InnerDefaultProperty,

    /// <summary>Specifies that the property persists as the only inner text of the ASP.NET server control. The property value is HTML encoded. Only a string can be given this designation.</summary>
    EncodedInnerDefaultProperty,
}

/// <summary>Defines the metadata attribute that specifies how an ASP.NET server control property or event is persisted to an ASP.NET page at design time. This class cannot be inherited.</summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.All)]
public sealed class PersistenceModeAttribute : Attribute
{
    /// <summary>Specifies that the property or event persists in the opening tag of the server control as an attribute. This field is read-only.</summary>
    public static readonly PersistenceModeAttribute Attribute = new(PersistenceMode.Attribute);

    /// <summary>Specifies that the property persists as a nested tag within the opening and closing tags of the server control. This field is read-only.</summary>
    public static readonly PersistenceModeAttribute InnerProperty = new(PersistenceMode.InnerProperty);

    /// <summary>Specifies that a property persists as the only inner content of the ASP.NET server control. This field is read-only.</summary>
    public static readonly PersistenceModeAttribute InnerDefaultProperty = new(PersistenceMode.InnerDefaultProperty);

    /// <summary>Specifies that a property is HTML-encoded and persists as the only inner content of the ASP.NET server control. This field is read-only.</summary>
    public static readonly PersistenceModeAttribute EncodedInnerDefaultProperty = new(PersistenceMode.EncodedInnerDefaultProperty);

    /// <summary>Specifies the default type for the <see cref="T:System.Web.UI.PersistenceModeAttribute" /> class. The default is PersistenceMode.Attribute. This field is read-only.</summary>
    public static readonly PersistenceModeAttribute Default = Attribute;

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.PersistenceModeAttribute" /> class. </summary>
    /// <param name="mode">The <see cref="T:System.Web.UI.PersistenceMode" /> value to assign to <see cref="P:System.Web.UI.PersistenceModeAttribute.Mode" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="mode" /> is not one of the <see cref="T:System.Web.UI.PersistenceMode" /> values.</exception>
    public PersistenceModeAttribute(PersistenceMode mode)
    {
        Mode = mode is >= PersistenceMode.Attribute and <= PersistenceMode.EncodedInnerDefaultProperty
            ? mode
            : throw new ArgumentOutOfRangeException(nameof(mode));
    }

    /// <summary>Gets the current value of the <see cref="T:System.Web.UI.PersistenceMode" /> enumeration.</summary>
    /// <returns>A <see cref="T:System.Web.UI.PersistenceMode" /> that represents the current value of the enumeration. This value can be Attribute, InnerProperty, InnerDefaultProperty, or EncodedInnerDefaultProperty. The default is Attribute.</returns>
    public PersistenceMode Mode { get; }

    /// <summary>Provides a hash value for a <see cref="T:System.Web.UI.PersistenceModeAttribute" /> attribute.</summary>
    /// <returns>The hash value to be assigned to the <see cref="T:System.Web.UI.PersistenceModeAttribute" />.</returns>
    public override int GetHashCode() => Mode.GetHashCode();

    /// <summary>Compares the <see cref="T:System.Web.UI.PersistenceModeAttribute" /> object against another object.</summary>
    /// <returns>true if the objects are considered equal; otherwise, false.</returns>
    /// <param name="obj">The object to compare to.</param>
    public override bool Equals(object? obj)
    {
        if (obj == this)
            return true;
        return obj is PersistenceModeAttribute attribute && attribute.Mode == Mode;
    }

    /// <summary>Indicates whether the <see cref="T:System.Web.UI.PersistenceModeAttribute" /> object is of the default type.</summary>
    /// <returns>true if the <see cref="T:System.Web.UI.PersistenceModeAttribute" /> is of the default type; otherwise, false.</returns>
    public override bool IsDefaultAttribute() => Equals(Default);
}

/// <summary>Declares the base type of the container control of a property that returns an <see cref="T:System.Web.UI.ITemplate" /> interface and is marked with the <see cref="T:System.Web.UI.TemplateContainerAttribute" /> attribute. The control with the <see cref="T:System.Web.UI.ITemplate" /> property must implement the <see cref="T:System.Web.UI.INamingContainer" /> interface. This class cannot be inherited.</summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property)]
public sealed class TemplateContainerAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.TemplateContainerAttribute" /> class using the specified container type.</summary>
    /// <param name="containerType">The <see cref="T:System.Type" /> for the container control. </param>
    public TemplateContainerAttribute(Type containerType)
        : this(containerType, BindingDirection.OneWay)
    {
    }

    /// <summary>Gets the binding direction of the container control.</summary>
    /// <returns>A <see cref="T:System.ComponentModel.BindingDirection" /> indicating the container control's binding direction. The default is <see cref="F:System.ComponentModel.BindingDirection.OneWay" />.</returns>
    public BindingDirection BindingDirection { get; }

    /// <summary>Gets the container control type.</summary>
    /// <returns>The container control <see cref="T:System.Type" />.</returns>
    public Type ContainerType { get; }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.TemplateContainerAttribute" /> class using the specified container type and the <see cref="P:System.Web.UI.TemplateContainerAttribute.BindingDirection" /> property.</summary>
    /// <param name="containerType">The <see cref="T:System.Type" /> for the container control.</param>
    /// <param name="bindingDirection">The <see cref="P:System.Web.UI.TemplateContainerAttribute.BindingDirection" /> for the container control.</param>
    public TemplateContainerAttribute(Type containerType, BindingDirection bindingDirection)
    {
        ContainerType = containerType;
        BindingDirection = bindingDirection;
    }
}