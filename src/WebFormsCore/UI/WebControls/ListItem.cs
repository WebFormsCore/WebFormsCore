using System;
using System.ComponentModel;

namespace WebFormsCore.UI.WebControls;

public sealed partial class ListItem : IAttributeAccessor
{
    [ViewState] private bool _selected;
    [ViewState] private bool _enabled;
    [ViewState] private string? _text;
    [ViewState] private string? _value;
    [ViewState] private AttributeCollection _attributes = new();

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.ListItem" /> class.</summary>
    public ListItem()
        : this(null, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.ListItem" /> class with the specified text data.</summary>
    /// <param name="text">The text to display in the list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" />. </param>
    public ListItem(string? text)
        : this(text, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.ListItem" /> class with the specified text and value data.</summary>
    /// <param name="text">The text to display in the list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" />. </param>
    /// <param name="value">The value associated with the <see cref="T:System.Web.UI.WebControls.ListItem" />. </param>
    public ListItem(string? text, string? value)
        : this(text, value, true)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.ListItem" /> class with the specified text, value, and enabled data.</summary>
    /// <param name="text">The text to display in the list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" />.</param>
    /// <param name="value">The value associated with the <see cref="T:System.Web.UI.WebControls.ListItem" />.</param>
    /// <param name="enabled">Indicates whether the <see cref="T:System.Web.UI.WebControls.ListItem" /> is enabled.</param>
    public ListItem(string? text, string? value, bool enabled)
    {
        _text = text;
        _value = value;
        _enabled = enabled;
    }

    /// <summary>Gets a collection of attribute name and value pairs for the <see cref="T:System.Web.UI.WebControls.ListItem" /> that are not directly supported by the class.</summary>
    /// <returns>A <see cref="T:System.Web.UI.AttributeCollection" /> that contains a collection of name and value pairs.</returns>
    public AttributeCollection Attributes => _attributes;

    /// <summary>Gets or sets a value indicating whether the list item is enabled.</summary>
    /// <returns>true if the list item is enabled; otherwise, false. The default is true.</returns>
    [DefaultValue(true)]
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; }
    }

    internal bool HasAttributes => _attributes != null && _attributes.Count > 0;

    /// <summary>Gets or sets a value indicating whether the item is selected.</summary>
    /// <returns>true if the item is selected; otherwise, false. The default is false.</returns>
    [DefaultValue(false)]
    [TypeConverter(typeof(MinimizableAttributeTypeConverter))]
    public bool Selected
    {
        get => _selected;
        set => _selected = value;
    }

    /// <summary>Gets or sets the text displayed in a list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" />.</summary>
    /// <returns>The text displayed in a list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" /> control. The default value is <see cref="F:System.String.Empty" />.</returns>
    [Localizable(true)]
    [DefaultValue("")]
    public string Text
    {
        get
        {
            if (_text != null)
            {
                return _text;
            }

            return _value ?? string.Empty;
        }
        set => _text = value;
    }

    /// <summary>Gets or sets the value associated with the <see cref="T:System.Web.UI.WebControls.ListItem" />.</summary>
    /// <returns>The value associated with the <see cref="T:System.Web.UI.WebControls.ListItem" />. The default is <see cref="F:System.String.Empty" />.</returns>
    [Localizable(true)]
    [DefaultValue("")]
    public string Value
    {
        get
        {
            if (_value != null)
            {
                return _value;
            }

            return _text ?? string.Empty;
        }
        set => _value = value;
    }

    /// <summary>Serves as a hash function for a particular type, and is suitable for use in hashing algorithms and data structures like a hash table.</summary>
    public override int GetHashCode() => HashCode.Combine(Value.GetHashCode(), Text.GetHashCode());

    /// <summary>Determines whether the specified object has the same value and text as the current list item.</summary>
    /// <returns>true if the specified object is equivalent to the current list item; otherwise, false.</returns>
    /// <param name="o">The object to compare with the current list item.</param>
    public override bool Equals(object? o) => o is ListItem listItem && Value.Equals(listItem.Value) && Text.Equals(listItem.Text);

    /// <summary>Creates a <see cref="T:System.Web.UI.WebControls.ListItem" /> from the specified text.</summary>
    /// <returns>A <see cref="T:System.Web.UI.WebControls.ListItem" /> that represents the text specified by the <paramref name="s" /> parameter.</returns>
    /// <param name="s">The text to display in the list control for the item represented by the <see cref="T:System.Web.UI.WebControls.ListItem" />. </param>
    public static ListItem FromString(string s) => new ListItem(s);

    /// <returns>A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.</returns>
    public override string ToString() => Text;

    /// <summary>Returns the attribute value of the list item control having the specified attribute name.</summary>
    /// <returns>The value of the specified attribute.</returns>
    /// <param name="name">The name component of an attribute's name/value pair. </param>
    string? IAttributeAccessor.GetAttribute(string name) => Attributes[name];

    /// <summary>Sets an attribute of the list item control with the specified name and value.</summary>
    /// <param name="name">The name component of the attribute's name/value pair. </param>
    /// <param name="value">The value component of the attribute's name/value pair. </param>
    void IAttributeAccessor.SetAttribute(string name, string? value) => Attributes[name] = value;
}
