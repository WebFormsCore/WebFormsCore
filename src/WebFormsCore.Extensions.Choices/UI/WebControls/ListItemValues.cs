using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI.WebControls;

internal class ListItemValues : ICollection<string>
{
    private readonly List<ListItem> _items;

    public ListItemValues(List<ListItem> items)
    {
        _items = items;
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _items.Select(x => x.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private bool TryFindItem(string item, [NotNullWhen(true)] out ListItem? result)
    {
        foreach (var listItem in _items)
        {
            if (listItem.Value == item)
            {
                result = listItem;
                return true;
            }
        }

        result = null;
        return false;
    }

    public void Add(string item)
    {
        if (!TryFindItem(item, out var result))
        {
            throw new ArgumentOutOfRangeException(nameof(item));
        }

        result.Selected = true;
    }

    public void Clear()
    {
        foreach (var listItem in _items)
        {
            listItem.Selected = false;
        }
    }

    public bool Contains(string item)
    {
        return TryFindItem(item, out var result) && result.Selected;
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        foreach (var listItem in _items)
        {
            if (listItem.Selected)
            {
                array[arrayIndex++] = listItem.Value;
            }
        }
    }

    public bool Remove(string item)
    {
        if (!TryFindItem(item, out var result))
        {
            return false;
        }

        result.Selected = false;
        return true;
    }

    public int Count => _items.Count(x => x.Selected);

    public bool IsReadOnly => false;
}
