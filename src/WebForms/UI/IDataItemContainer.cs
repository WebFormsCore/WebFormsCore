namespace System.Web.UI;

/// <summary>Enables data-bound control containers to identify a data item object for simplified data-binding operations.</summary>
public interface IDataItemContainer : INamingContainer
{
    /// <summary>When implemented, gets an <see langword="object" /> that is used in simplified data-binding operations.</summary>
    /// <returns>An <see langword="object" /> that represents the value to use when data-binding operations are performed.</returns>
    object DataItem { get; }

    /// <summary>When implemented, gets the index of the data item bound to a control.</summary>
    /// <returns>An <see langword="Integer" /> representing the index of the data item in the data source.</returns>
    int DataItemIndex { get; }

    /// <summary>When implemented, gets the position of the data item as displayed in a control.</summary>
    /// <returns>An <see langword="Integer" /> representing the position of the data item as displayed in a control.</returns>
    int DisplayIndex { get; }
}
