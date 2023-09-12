using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebFormsCore.UI;

public sealed class GridKeyCollection : IViewStateObject, IDisposable
{
    private int _cachedPropertyCount;
    private int _keyCount;
    private PropertyInfo[]? _cachedProperties;
    private readonly Grid _grid;
    private object?[]? _keys;
    private bool _isFromViewState;

    public GridKeyCollection(Grid grid)
    {
        _grid = grid;
    }

    public bool WriteToViewState => _grid is { ItemType: not null, DataKeys.Length: > 0 };

    private bool IsCacheValid
    {
        [MemberNotNullWhen(true, nameof(_cachedProperties))]
        get
        {
            if (_cachedProperties == null || _cachedPropertyCount != _grid.DataKeys.Length)
            {
                return false;
            }

            for (var i = 0; i < _cachedPropertyCount; i++)
            {
                if (_cachedProperties[i].Name != _grid.DataKeys[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    public List<T> GetAll<T>(string name)
    {
        var index = GetIndex(name);
        var keys = new List<T>();

        if (_keys is null)
        {
            return keys;
        }

        var length = _grid.DataKeys.Length;
        var span = _keys.AsSpan();

        for (var i = 0; i < _keyCount; i++)
        {
            keys.Add((T) span[i * length + index]!);
        }

        return keys;
    }

    private int GetIndex(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var length = _grid.DataKeys.Length;

        for (var i = 0; i < length; i++)
        {
            if (name.Equals(_grid.DataKeys[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"The property '{name}' does not exist on type '{_grid.ItemType?.FullName}'.");
    }

    public void TrackViewState(ViewStateProvider provider)
    {
    }

    public void WriteViewState(ref ViewStateWriter writer)
    {
        var properties = GetProperties();

        if (_keys is null)
        {
            writer.Write((ushort) 0);
            return;
        }

        var span = _keys.AsSpan();

        writer.Write((ushort) _keyCount);

        for (var i = 0; i < _keyCount; i++)
        {
            var offset = i * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                writer.WriteObject(properties[j].PropertyType, span[offset + j]);
            }
        }
    }

    public void ReadViewState(ref ViewStateReader reader)
    {
        ReturnKeysToCache();

        var length = reader.Read<ushort>();

        if (length == 0)
        {
            return;
        }

        _isFromViewState = true;
        _keys = ArrayPool<object?>.Shared.Rent(length);
        _keyCount = length;

        var properties = GetProperties();
        var span = _keys.AsSpan();

        for (var i = 0; i < length; i++)
        {
            var offset = i * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                span[offset + j] = reader.ReadObject(properties[j].PropertyType);
            }
        }
    }

    internal void Store()
    {
        if (_grid.DataKeys.Length == 0)
        {
            return;
        }

        ReturnKeysToCache();

        var properties = GetProperties();

        _isFromViewState = false;
        _keyCount = _grid.Items.Count;
        _keys = ArrayPool<object?>.Shared.Rent(_keyCount * properties.Length);

        var span = _keys.AsSpan();

        for (var i = 0; i < _grid.Items.Count; i++)
        {
            var item = _grid.Items[i];
            var offset = i * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                var value = property.GetValue(item.DataItem);
                span[offset + j] = value;
            }
        }
    }

    internal void Validate()
    {
        if (_grid.DataKeys.Length == 0)
        {
            return;
        }

        if (!_isFromViewState)
        {
            return;
        }

        var properties = GetProperties();
        var span = _keys.AsSpan();

        for (var i = 0; i < _grid.Items.Count; i++)
        {
            var item = _grid.Items[i];
            var offset = i * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                var value = property.GetValue(item.DataItem);

                if (!Equals(value, span[offset + j]))
                {
                    throw new InvalidOperationException($"The value of the property '{property.Name}' on item '{i}' does not match the value in the view state.");
                }
            }
        }
    }

    private ReadOnlySpan<PropertyInfo> GetProperties()
    {
        if (IsCacheValid)
        {
            return _cachedProperties.AsSpan(0, _cachedPropertyCount);
        }

        if (_cachedProperties is not null)
        {
            ArrayPool<PropertyInfo>.Shared.Return(_cachedProperties, true);
        }

        var properties = ArrayPool<PropertyInfo>.Shared.Rent(_grid.DataKeys.Length);

        for (var i = 0; i < _grid.DataKeys.Length; i++)
        {
            var item = _grid.DataKeys[i];
            var property = _grid.ItemType!.GetProperty(item);

            properties[i] = property ?? throw new InvalidOperationException($"The property '{item}' does not exist on type '{_grid.ItemType.FullName}'.");
        }

        _cachedProperties = properties;
        _cachedPropertyCount = _grid.DataKeys.Length;

        return properties.AsSpan(0, _cachedPropertyCount);
    }

    private void ReturnKeysToCache()
    {
        if (_keys is null) return;

        ArrayPool<object?>.Shared.Return(_keys, true);
        _keys = null;
    }

    public void Dispose()
    {
        ReturnKeysToCache();

        if (_cachedProperties is not null)
        {
            ArrayPool<PropertyInfo>.Shared.Return(_cachedProperties, true);
        }
    }
}
