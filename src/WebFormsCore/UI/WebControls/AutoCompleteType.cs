namespace WebFormsCore.UI.WebControls;

/// <summary>Represents the values that control the behavior of the AutoComplete feature in a <see cref="T:System.Web.UI.WebControls.TextBox" /> control.</summary>
public enum AutoCompleteType
{
    /// <summary>No category is associated with the <see cref="T:System.Web.UI.WebControls.TextBox" /> control. All <see cref="T:System.Web.UI.WebControls.TextBox" /> controls with the same <see cref="P:System.Web.UI.Control.ID" /> share the same value list.</summary>
    None,
    /// <summary>The AutoComplete feature is disabled for the <see cref="T:System.Web.UI.WebControls.TextBox" /> control.</summary>
    Disabled,
    /// <summary>The phone number for a mobile-phone category.</summary>
    Cellular,
    /// <summary>The name of a business category.</summary>
    Company,
    /// <summary>A department within a business category.</summary>
    Department,
    /// <summary>The name to display for the user category.</summary>
    DisplayName,
    /// <summary>The user's e-mail address category.</summary>
    Email,
    /// <summary>The first name category.</summary>
    FirstName,
    /// <summary>The gender of the user category.</summary>
    Gender,
    /// <summary>The city for a home address category.</summary>
    HomeCity,
    /// <summary>The country/region for a home address category.</summary>
    HomeCountryRegion,
    /// <summary>The fax number for a home address category.</summary>
    HomeFax,
    /// <summary>The phone number for a home address category.</summary>
    HomePhone,
    /// <summary>The state for a home address category.</summary>
    HomeState,
    /// <summary>The street for a home address category.</summary>
    HomeStreetAddress,
    /// <summary>The ZIP code for a home address category.</summary>
    HomeZipCode,
    /// <summary>The URL to a Web site category.</summary>
    Homepage,
    /// <summary>The user's job title category.</summary>
    JobTitle,
    /// <summary>The last name category.</summary>
    LastName,
    /// <summary>The user's middle name category.</summary>
    MiddleName,
    /// <summary>Any supplemental information to include in the form category.</summary>
    Notes,
    /// <summary>The location of the business office category.</summary>
    Office,
    /// <summary>The phone number for a pager category.</summary>
    Pager,
    /// <summary>The city for a business address category.</summary>
    BusinessCity,
    /// <summary>The country/region for a business address category.</summary>
    BusinessCountryRegion,
    /// <summary>The fax number for a business address category.</summary>
    BusinessFax,
    /// <summary>The phone number for a business address category.</summary>
    BusinessPhone,
    /// <summary>The state for a business address category.</summary>
    BusinessState,
    /// <summary>The street for a business address category.</summary>
    BusinessStreetAddress,
    /// <summary>The URL to a business Web site category.</summary>
    BusinessUrl,
    /// <summary>The ZIP code for a business address category.</summary>
    BusinessZipCode,
    /// <summary>The keyword or keywords with which to search a Web page or Web site category.</summary>
    Search,
    /// <summary>The AutoComplete feature is enabled for the <see cref="T:System.Web.UI.WebControls.TextBox" /> control.</summary>
    Enabled,
}
