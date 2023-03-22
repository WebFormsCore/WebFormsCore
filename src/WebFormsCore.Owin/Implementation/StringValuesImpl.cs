using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HeaderDictionaryImpl : IDictionary<string, StringValues>, IReadOnlyDictionary<string, StringValues>
{
    private IDictionary<string, string[]> _dictionary;

    public void SetNameValueCollection(IDictionary<string, string[]> nameValueCollection)
    {
        _dictionary = nameValueCollection;
    }

    public void Reset()
    {
        _dictionary = null!;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _dictionary
            .Select(kv => new KeyValuePair<string, StringValues>(kv.Key, new StringValues(kv.Value)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, StringValues> item)
    {
        _dictionary.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        return _dictionary.ContainsKey(item.Key) && _dictionary[item.Key] == item.Value;
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
            _dictionary.Remove(item.Key);
            return true;
        }

        return false;
    }

    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;
    public bool ContainsKey(string key)
    {
        return _dictionary.ContainsKey(key);
    }

    public void Add(string key, StringValues value)
    {
        _dictionary.Add(key, value);
    }

    public bool Remove(string key)
    {
        if (ContainsKey(key))
        {
            _dictionary.Remove(key);
            return true;
        }

        return false;
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        if (ContainsKey(key))
        {
            value = _dictionary[key];
            return true;
        }

        value = default;
        return false;
    }

    public StringValues this[string key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    IEnumerable<string> IReadOnlyDictionary<string, StringValues>.Keys => Keys;

    IEnumerable<StringValues> IReadOnlyDictionary<string, StringValues>.Values => Values;

    public ICollection<string> Keys => _dictionary.Keys;

    public ICollection<StringValues> Values => _dictionary.Values
        .Select(v => new StringValues(v))
        .ToList();
}
