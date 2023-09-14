using System;
using System.Diagnostics;

namespace WebFormsCore.UI;

public interface INeedDataSourceProvider
{
    bool IgnorePaging { get; set; }
}

public class NeedDataSourceEventArgs : EventArgs
{
    private readonly INeedDataSourceProvider _dataKeyProvider;
    private readonly bool _filterByKeys;

    public NeedDataSourceEventArgs(INeedDataSourceProvider dataKeyProvider, bool filterByKeys)
    {
        _filterByKeys = filterByKeys;
        _dataKeyProvider = dataKeyProvider;
    }

    /// <summary>
    /// Check if the data source should be filtered by the keys.
    /// If this property is accessed, it is assumed that the data source will be filtered by the keys and not by the current page.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool FilterByKeys
    {
        get
        {
            if (_filterByKeys)
            {
                // Assume that the grid will be filtered by keys
                _dataKeyProvider.IgnorePaging = true;
            }

            return _filterByKeys;
        }
    }
}
