using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class QueryCollectionDictionary : IReadOnlyDictionary<string, StringValues>
{
    private IQueryCollection _queryCollection = default!;

    public void Reset()
    {
        _queryCollection = null!;
    }

    public void SetQueryCollection(IQueryCollection nameValueCollection)
    {
        _queryCollection = nameValueCollection;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _queryCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _queryCollection.Count;
    public bool ContainsKey(string key)
    {
        return _queryCollection.ContainsKey(key);
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        return _queryCollection.TryGetValue(key, out value);
    }

    public StringValues this[string key] => _queryCollection[key];

    public IEnumerable<string> Keys => _queryCollection.Keys;
    public IEnumerable<StringValues> Values => _queryCollection.Keys.Select(key => _queryCollection[key]);
}
