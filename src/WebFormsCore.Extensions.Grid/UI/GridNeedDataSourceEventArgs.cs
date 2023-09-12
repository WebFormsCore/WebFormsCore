using System.Diagnostics;

namespace WebFormsCore.UI;

public class GridNeedDataSourceEventArgs : EventArgs
{
    private readonly Grid _grid;
    private readonly bool _filterByKeys;

    public GridNeedDataSourceEventArgs(Grid grid, bool filterByKeys)
    {
        _filterByKeys = filterByKeys;
        _grid = grid;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool FilterByKeys
    {
        get
        {
            if (_filterByKeys)
            {
                // Assume that the grid will be filtered by keys
                _grid.IgnorePaging = true;
            }

            return _filterByKeys;
        }
    }
}
