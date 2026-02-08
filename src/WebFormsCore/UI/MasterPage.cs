using System;
using System.Collections.Generic;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

/// <summary>
/// Represents a master page that provides a layout template for content pages.
/// </summary>
[ParseChildren(false)]
public class MasterPage : Control, INamingContainer
{
    private Dictionary<string, ContentPlaceHolder>? _contentPlaceHolders;

    /// <summary>
    /// Gets the page that owns this master page.
    /// </summary>
    public Page? OwnerPage { get; internal set; }

    internal void RegisterContentPlaceHolder(ContentPlaceHolder placeHolder)
    {
        if (placeHolder.ID is null) return;

        _contentPlaceHolders ??= new Dictionary<string, ContentPlaceHolder>(StringComparer.OrdinalIgnoreCase);
        _contentPlaceHolders[placeHolder.ID] = placeHolder;
    }

    internal ContentPlaceHolder? FindContentPlaceHolder(string id)
    {
        if (_contentPlaceHolders is null) return null;

        _contentPlaceHolders.TryGetValue(id, out var result);
        return result;
    }
}
