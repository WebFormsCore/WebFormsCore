using System.Diagnostics.CodeAnalysis;

#if NET
// ReSharper disable once CheckNamespace
namespace System.Web.UI;

/// <summary>Defines a metadata attribute that you can use when developing ASP.NET server controls. Use the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class to indicate how the page parser should treat content nested inside a server control tag declared on a page. This class cannot be inherited.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ParseChildrenAttribute : Attribute
{
    /// <summary>Indicates that the nested content that is contained within the server control is parsed as controls.</summary>
    public static readonly ParseChildrenAttribute ParseAsChildren = new ParseChildrenAttribute(false, false);

    /// <summary>Indicates that the nested content that is contained within a server control is parsed as properties of the control. </summary>
    public static readonly ParseChildrenAttribute ParseAsProperties = new ParseChildrenAttribute(true, false);

    /// <summary>Defines the default value for the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class. This field is read-only.</summary>
    public static readonly ParseChildrenAttribute Default = ParseAsChildren;

    private bool _childrenAsProps;
    private string? _defaultProperty;
    private readonly Type? _childControlType;
    private readonly bool _allowChanges = true;

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class.</summary>
    public ParseChildrenAttribute()
        : this(false, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class using the <see cref="P:System.Web.UI.ParseChildrenAttribute.ChildrenAsProperties" /> property to determine if the elements that are contained within a server control are parsed as properties of the server control.</summary>
    /// <param name="childrenAsProperties">true to parse the elements as properties of the server control; otherwise, false. </param>
    public ParseChildrenAttribute(bool childrenAsProperties)
        : this(childrenAsProperties, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class using the <see cref="P:System.Web.UI.ParseChildrenAttribute.ChildControlType" /> property to determine which elements that are contained within a server control are parsed as controls.</summary>
    /// <param name="childControlType">The control type to parse as a property. </param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="childControlType" /> is null. </exception>
    public ParseChildrenAttribute(Type? childControlType)
        : this(false, null)
    {
        _childControlType = childControlType ?? throw new ArgumentNullException(nameof(childControlType));
    }

    private ParseChildrenAttribute(bool childrenAsProperties, bool allowChanges)
        : this(childrenAsProperties, null)
    {
        _allowChanges = allowChanges;
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class using the <paramref name="childrenAsProperties" /> and <paramref name="defaultProperty" /> parameters.</summary>
    /// <param name="childrenAsProperties">true to parse the elements as properties of the server control; otherwise, false. </param>
    /// <param name="defaultProperty">A string that defines a collection property of the server control into which nested content is parsed by default. </param>
    public ParseChildrenAttribute(bool childrenAsProperties, string? defaultProperty)
    {
        _childrenAsProps = childrenAsProperties;
        if (!_childrenAsProps)
            return;
        _defaultProperty = defaultProperty;
    }

    /// <summary>Gets a value indicating the allowed type of a control. </summary>
    /// <returns>The control type. The default is <see cref="T:System.Web.UI.Control" />. </returns>
    public Type ChildControlType => _childControlType ?? typeof(Control);

    /// <summary>Gets or sets a value indicating whether to parse the elements that are contained within a server control as properties.</summary>
    /// <returns>true to parse the elements as properties; otherwise, false. The default is true.</returns>
    /// <exception cref="T:System.NotSupportedException">The current <see cref="T:System.Web.UI.ParseChildrenAttribute" /> was invoked with <paramref name="childrenAsProperties" /> set to false.</exception>
    public bool ChildrenAsProperties
    {
        get => _childrenAsProps;
        set
        {
            if (!_allowChanges)
                throw new NotSupportedException();
            _childrenAsProps = value;
        }
    }

    /// <summary>Gets or sets the default property for the server control into which the elements are parsed.</summary>
    /// <returns>The name of the default collection property of the server control into which the elements are parsed.</returns>
    /// <exception cref="T:System.NotSupportedException">The current <see cref="T:System.Web.UI.ParseChildrenAttribute" /> was invoked with <paramref name="childrenAsProperties" /> set to false.</exception>
    public string DefaultProperty
    {
        get => _defaultProperty ?? string.Empty;
        set
        {
            if (!_allowChanges)
                throw new NotSupportedException();
            _defaultProperty = value;
        }
    }

    /// <summary>Serves as a hash function for the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> object.</summary>
    /// <returns>A hash code for the current <see cref="T:System.Web.UI.ParseChildrenAttribute" /> object. </returns>
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() => !_childrenAsProps
        ? HashCode.Combine(_childrenAsProps.GetHashCode(), _childControlType?.GetHashCode() ?? 0)
        : HashCode.Combine(_childrenAsProps.GetHashCode(), DefaultProperty.GetHashCode());

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <returns>true if <paramref name="obj" /> is equal to the current object; otherwise, false.</returns>
    /// <param name="obj">The object to compare with the current object.</param>
    public override bool Equals(object? obj)
    {
        if (obj == this)
            return true;

        if (obj is not ParseChildrenAttribute childrenAttribute)
            return false;

        return !_childrenAsProps
            ? !childrenAttribute.ChildrenAsProperties && childrenAttribute._childControlType == _childControlType
            : childrenAttribute.ChildrenAsProperties && DefaultProperty.Equals(childrenAttribute.DefaultProperty);
    }

    /// <summary>Returns a value indicating whether the value of the current instance of the <see cref="T:System.Web.UI.ParseChildrenAttribute" /> class is the default value of the derived class.</summary>
    /// <returns>true if the current <see cref="T:System.Web.UI.ParseChildrenAttribute" /> value is the default instance; otherwise, false.</returns>
    public override bool IsDefaultAttribute() => Equals(Default);
}
#endif
