using System.ComponentModel;

namespace System.Web;

/// <internalonly/>
/// <devdoc>
///    <para>
///       CategoryAttribute that can access ASP.NET localized strings.
///    </para>
/// </devdoc>
[AttributeUsage(AttributeTargets.All)]
internal sealed class WebCategoryAttribute : CategoryAttribute {

    /// <devdoc>
    ///    <para>
    ///       Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/> class.
    ///    </para>
    /// </devdoc>
    internal WebCategoryAttribute(string category) : base(category) {
    }

    public override object TypeId => typeof(CategoryAttribute);


    /// <devdoc>
    ///     This method is called the first time the category property
    ///     is accessed.  It provides a way to lookup a localized string for
    ///     the given category.  Classes may override this to add their
    ///     own localized names to categories.  If a localized string is
    ///     available for the given value, the method should return it.
    ///     Otherwise, it should return null.
    /// </devdoc>
    protected override string? GetLocalizedString(string value)
    {
        var localizedValue = base.GetLocalizedString(value) ?? SR.GetString("Category_" + value);

        // This attribute is internal, and we should never have a missing resource string.
        //
        return localizedValue;
    }
}
