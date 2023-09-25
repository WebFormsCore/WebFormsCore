using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace WebFormsCore.UI.WebControls;

public class ListItemValues : ICollection<string>
{
    private readonly List<ListItem> _items;

    public ListItemValues(List<ListItem> items)
    {
        _items = items;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_items.GetEnumerator());
    }

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool SequenceEqual(ReadOnlySpan<string> other)
    {
        if (other.Length != _items.Count)
        {
            return false;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Value != other[i])
            {
                return false;
            }
        }

        return true;
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

    public override string ToString()
    {
        if (Count == 0)
        {
            return string.Empty;
        }

        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = GetEnumerator();

        if (Count == 1)
        {
            enumerator.MoveNext();
            return enumerator.Current;
        }

        var builder = new StringBuilder();

        enumerator.MoveNext();
        builder.Append(enumerator.Current);

        while (enumerator.MoveNext())
        {
            builder.Append(',');
            builder.Append(enumerator.Current);
        }

        return builder.ToString();
    }

    public struct Enumerator : IEnumerator<string>
    {
        private List<ListItem>.Enumerator _enumerator;

        public Enumerator(List<ListItem>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
        }

        object IEnumerator.Current => Current;

        public string Current => _enumerator.Current!.Value;

        void IDisposable.Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
