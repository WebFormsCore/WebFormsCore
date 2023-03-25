using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class NameValueDictionary : IDictionary<string, StringValues>, IReadOnlyDictionary<string, StringValues>
{
    private NameValueCollection _nameValueCollection;

    public void SetNameValueCollection(NameValueCollection nameValueCollection)
    {
        _nameValueCollection = nameValueCollection;
    }

    public void Reset()
    {
        _nameValueCollection = null!;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _nameValueCollection.AllKeys
            .Select(key => new KeyValuePair<string, StringValues>(key, _nameValueCollection[(string)key]))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, StringValues> item)
    {
        _nameValueCollection.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _nameValueCollection.Clear();
    }

    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        return _nameValueCollection.AllKeys.Contains(item.Key) && _nameValueCollection[item.Key] == item.Value;
    }

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        if (Contains(item))
        {
            _nameValueCollection.Remove(item.Key);
            return true;
        }

        return false;
    }

    public int Count => _nameValueCollection.Count;
    public bool IsReadOnly => false;
    public bool ContainsKey(string key)
    {
        return _nameValueCollection.AllKeys.Contains(key);
    }

    public void Add(string key, StringValues value)
    {
        _nameValueCollection.Add(key, value);
    }

    public bool Remove(string key)
    {
        if (ContainsKey(key))
        {
            _nameValueCollection.Remove(key);
            return true;
        }

        return false;
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        if (ContainsKey(key))
        {
            value = _nameValueCollection[key];
            return true;
        }

        value = default;
        return false;
    }

    public StringValues this[string key]
    {
        get => _nameValueCollection[key];
        set => _nameValueCollection[key] = value;
    }

    IEnumerable<string> IReadOnlyDictionary<string, StringValues>.Keys => Keys;

    IEnumerable<StringValues> IReadOnlyDictionary<string, StringValues>.Values => Values;

    public ICollection<string> Keys => _nameValueCollection.AllKeys;

    public ICollection<StringValues> Values => _nameValueCollection.AllKeys
        .Select(key => (StringValues)_nameValueCollection[key])
        .ToList();
}
