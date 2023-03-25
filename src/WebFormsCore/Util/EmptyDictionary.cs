using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WebFormsCore;

public class EmptyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    public static readonly IReadOnlyDictionary<TKey, TValue> Instance = new EmptyDictionary<TKey, TValue>();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => 0;
    public bool ContainsKey(TKey key) => false;

    public bool TryGetValue(TKey key, out TValue value)
    {
        value = default!;
        return false;
    }

    public TValue this[TKey key] => throw new KeyNotFoundException();

    public IEnumerable<TKey> Keys => Enumerable.Empty<TKey>();

    public IEnumerable<TValue> Values => Enumerable.Empty<TValue>();
}
