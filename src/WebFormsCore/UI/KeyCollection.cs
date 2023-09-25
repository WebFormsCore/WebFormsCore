using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebFormsCore.UI;

public interface IDataKeyProvider
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    Type? ItemType { get; }

    int ItemCount { get; }

    IEnumerable<IDataItemContainer> Items { get; }

    string[] DataKeys { get; }
}

public sealed class KeyCollection : IViewStateObject, IDisposable
{
    private int _cachedPropertyCount;
    private int _keyCount;
    private PropertyInfo[]? _cachedProperties;
    private readonly IDataKeyProvider _dataKeyProvider;
    private object?[]? _keys;
    private bool _isFromViewState;

    public KeyCollection(IDataKeyProvider dataKeyProvider)
    {
        _dataKeyProvider = dataKeyProvider;
    }

    public bool WriteToViewState => _dataKeyProvider is { ItemType: not null, DataKeys.Length: > 0 };

    private bool IsCacheValid
    {
        [MemberNotNullWhen(true, nameof(_cachedProperties))]
        get
        {
            if (_cachedProperties == null || _cachedPropertyCount != _dataKeyProvider.DataKeys.Length)
            {
                return false;
            }

            for (var i = 0; i < _cachedPropertyCount; i++)
            {
                if (_cachedProperties[i].Name != _dataKeyProvider.DataKeys[i])
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

        var length = _dataKeyProvider.DataKeys.Length;
        var span = _keys.AsSpan();

        for (var i = 0; i < _keyCount; i++)
        {
            keys.Add((T) span[i * length + index]!);
        }

        return keys;
    }

    public object Get(string name, int itemIndex)
    {
        var index = GetIndex(name);

        if (_keys is null)
        {
            return null!;
        }

        var length = _dataKeyProvider.DataKeys.Length;

        return _keys[itemIndex * length + index]!;
    }

    private int GetIndex(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var length = _dataKeyProvider.DataKeys.Length;

        for (var i = 0; i < length; i++)
        {
            if (name.Equals(_dataKeyProvider.DataKeys[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"The property '{name}' does not exist on type '{_dataKeyProvider.ItemType?.FullName}'.");
    }

    public void TrackViewState(ViewStateProvider provider)
    {
    }

    public void WriteViewState(ref ViewStateWriter writer)
    {
        var properties = GetProperties();

        if (_keyCount == 0 || properties.Length == 0)
        {
            writer.Write((ushort) 0);
            return;
        }

        writer.Write((ushort) _keyCount);

        var span = _keys.AsSpan(0, _keyCount * properties.Length);
        var propertyIndex = 0;

        foreach (var item in span)
        {
            var property = properties[propertyIndex++];

            if (propertyIndex == properties.Length)
            {
                propertyIndex = 0;
            }

            writer.WriteObject(property.PropertyType, item);
        }
    }

    public void ReadViewState(ref ViewStateReader reader)
    {
        ReturnKeysToCache();

        var keyCount = reader.Read<ushort>();

        if (keyCount == 0)
        {
            return;
        }

        var properties = GetProperties();
        var length = keyCount * properties.Length;

        _isFromViewState = true;
        _keys = ArrayPool<object?>.Shared.Rent(length);
        _keyCount = keyCount;

        var span = _keys.AsSpan();

        for (var i = 0; i < keyCount; i++)
        {
            var offset = i * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
#pragma warning disable IL2072
                span[offset + j] = reader.ReadObject(properties[j].PropertyType);
#pragma warning restore IL2072
            }
        }
    }

    public void Store()
    {
        if (_dataKeyProvider.DataKeys.Length == 0)
        {
            return;
        }

        ReturnKeysToCache();

        var properties = GetProperties();

        _isFromViewState = false;
        _keyCount = _dataKeyProvider.ItemCount;
        _keys = ArrayPool<object?>.Shared.Rent(_keyCount * properties.Length);

        var span = _keys.AsSpan();
        var index = 0;

        foreach(var item in _dataKeyProvider.Items)
        {
            var offset = index * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                var value = property.GetValue(item.DataItem);
                span[offset + j] = value;
            }

            index++;
        }
    }

    public void Validate()
    {
        if (_dataKeyProvider.DataKeys.Length == 0)
        {
            return;
        }

        if (!_isFromViewState)
        {
            return;
        }

        var properties = GetProperties();
        var span = _keys.AsSpan();
        var index = 0;

        foreach(var item in _dataKeyProvider.Items)
        {
            var offset = index * properties.Length;

            for (var j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                var value = property.GetValue(item.DataItem);

                if (!Equals(value, span[offset + j]))
                {
                    throw new InvalidOperationException($"The value of the property '{property.Name}' on item '{index}' does not match the value in the view state.");
                }
            }

            index++;
        }
    }

    private ReadOnlySpan<PropertyInfo> GetProperties()
    {
        if (_dataKeyProvider.DataKeys.Length == 0)
        {
            return ReadOnlySpan<PropertyInfo>.Empty;
        }

        if (IsCacheValid)
        {
            return _cachedProperties.AsSpan(0, _cachedPropertyCount);
        }

        if (_cachedProperties is not null)
        {
            ArrayPool<PropertyInfo>.Shared.Return(_cachedProperties, true);
        }

        var properties = ArrayPool<PropertyInfo>.Shared.Rent(_dataKeyProvider.DataKeys.Length);

        for (var i = 0; i < _dataKeyProvider.DataKeys.Length; i++)
        {
            var item = _dataKeyProvider.DataKeys[i];
            var property = _dataKeyProvider.ItemType!.GetProperty(item);

            properties[i] = property ?? throw new InvalidOperationException($"The property '{item}' does not exist on type '{_dataKeyProvider.ItemType.FullName}'.");
        }

        _cachedProperties = properties;
        _cachedPropertyCount = _dataKeyProvider.DataKeys.Length;

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
