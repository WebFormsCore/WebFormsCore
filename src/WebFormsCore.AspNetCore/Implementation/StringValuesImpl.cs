using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class StringValuesImpl : IReadOnlyDictionary<string, StringValues>
{
    private IFormCollection _formCollection = default!;

    public void Reset()
    {
        _formCollection = null!;
    }

    public void SetFormCollection(IFormCollection nameValueCollection)
    {
        _formCollection = nameValueCollection;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _formCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _formCollection.Count;
    public bool ContainsKey(string key)
    {
        return _formCollection.ContainsKey(key);
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        return _formCollection.TryGetValue(key, out value);
    }

    public StringValues this[string key] => _formCollection[key];

    public IEnumerable<string> Keys => _formCollection.Keys;
    public IEnumerable<StringValues> Values => _formCollection.Keys.Select(key => _formCollection[key]);
}
