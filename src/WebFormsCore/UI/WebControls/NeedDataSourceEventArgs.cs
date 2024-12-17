using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI;

public interface INeedDataSourceProvider
{
    bool IgnorePaging { get; set; }

    int PageIndex { get; set; }

    int? PageSize { get; set; }

    KeyCollection? Keys { get; }
}

public class NeedDataSourceEventArgs(INeedDataSourceProvider dataKeyProvider, bool filterByKeys) : EventArgs
{
    /// <summary>
    /// Check if the data source should be filtered by the keys.
    /// If this method is invoked, it is assumed that the data source will be filtered by the keys and not by the current page.
    /// </summary>
    public bool FilterByKeys<T>(string key, [NotNullWhen(true)] out List<T>? keys)
    {
        if (!filterByKeys || dataKeyProvider.Keys is null)
        {
            keys = null;
            return false;
        }

        // Assume that the grid will be filtered by keys
        dataKeyProvider.IgnorePaging = true;
        keys = dataKeyProvider.Keys.GetAll<T>(key);

        return true;
    }

    /// <summary>
    /// Check if the data source should be filtered by the current page.
    /// If this method is invoked, it is assumed that the data source will be filtered by the current page.
    /// </summary>
    public bool FilterByPage(out PageInfo info)
    {
        if (!dataKeyProvider.PageSize.HasValue)
        {
            info = default;
            return false;
        }

        dataKeyProvider.IgnorePaging = true;

        info = new PageInfo(dataKeyProvider.PageIndex, dataKeyProvider.PageSize.Value);

        return true;
    }
}

public readonly record struct PageInfo(int PageIndex, int PageSize)
{
    public int Offset => PageIndex * PageSize;
}
