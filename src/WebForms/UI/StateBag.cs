using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Web.UI;

public sealed class StateBag : IDictionary<string, object?>, IDictionary
{
    private readonly Dictionary<string, StateItem> _bag;
    private ValueCollection? _values;
    private bool _storeInView;

    public StateBag(bool ignoreCase)
    {
        _bag = new Dictionary<string, StateItem>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    public bool Contains(object key)
    {
        return _bag.ContainsKey((string)key);
    }

    void IDictionary.Remove(object key)
    {
        _bag.Remove((string)key);
    }

    bool IDictionary.IsFixedSize => false;

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_bag.GetEnumerator());
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, object?> item)
    {
        Add(item.Key, item.Value);
    }

    void IDictionary.Add(object key, object? value)
    {
        Add((string)key, value);
    }

    public void Clear()
    {
        _bag.Clear();
    }

    public bool Contains(KeyValuePair<string, object?> item)
    {
        return _bag.TryGetValue(item.Key, out var stateItem) && stateItem.Value == item.Value;
    }

    public bool Remove(KeyValuePair<string, object?> item)
    {
        return _bag.Remove(item.Key);
    }

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public int Count => _bag.Count;

    public int ViewStateCount => _bag.Count > 0 ? _bag.Count(i => i.Value.StoreInView) : 0;

    bool ICollection.IsSynchronized => ((ICollection)_bag).IsSynchronized;

    object ICollection.SyncRoot => ((ICollection)_bag).SyncRoot;

    bool IDictionary.IsReadOnly => ((IDictionary)_bag).IsReadOnly;

    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    object? IDictionary.this[object key]
    {
        get => this[(string)key];
        set => this[(string)key] = value;
    }

    public void Add(string key, object? value)
    {
        if (value == null && _storeInView)
        {
            _bag.Remove(key);
            return;
        }

        if (_bag.TryGetValue(key, out var stateItem))
        {
            stateItem.Value = value;
        }
        else
        {
            stateItem = new StateItem(value);
            _bag.Add(key, stateItem);
        }

        if (_storeInView)
        {
            stateItem.StoreInView = true;
        }
    }

    public bool ContainsKey(string key)
    {
        return _bag.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return _bag.Remove(key);
    }

    public bool TryGetValue(string key, out object? value)
    {
        if (_bag.TryGetValue(key, out var stateItem))
        {
            value = stateItem.Value;
            return true;
        }

        value = null;
        return false;
    }

    public object? this[string key]
    {
        get => _bag.TryGetValue(key, out var value) ? value.Value : null;
        set => Add(key, value);
    }

    public bool IsTracking => _storeInView;

    internal void TrackViewState() => _storeInView = true;

    public ICollection<string> Keys => _bag.Keys;

    ICollection IDictionary.Values => _values ??= new ValueCollection(_bag.Values);

    ICollection IDictionary.Keys => ((IDictionary)_bag).Keys;

    public ICollection<object?> Values => _values ??= new ValueCollection(_bag.Values);

    public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>, IDictionaryEnumerator
    {
        private Dictionary<string, StateItem>.Enumerator _enumerator;

        public Enumerator(Dictionary<string, StateItem>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            ((IEnumerator)_enumerator).Reset();
        }

        public KeyValuePair<string, object?> Current => new(_enumerator.Current.Key, _enumerator.Current.Value.Value);

        object? IEnumerator.Current => _enumerator.Current.Value.Value;

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        DictionaryEntry IDictionaryEnumerator.Entry => new(_enumerator.Current.Key, _enumerator.Current.Value.Value);

        object IDictionaryEnumerator.Key => _enumerator.Current.Key;

        object? IDictionaryEnumerator.Value => _enumerator.Current.Value.Value;
    }

    public sealed class ValueCollection : ICollection<object?>, ICollection, IReadOnlyCollection<object?>
    {
        private readonly Dictionary<string, StateItem>.ValueCollection _collection;

        public ValueCollection(Dictionary<string, StateItem>.ValueCollection collection)
        {
            _collection = collection;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_collection.GetEnumerator());
        }

        IEnumerator<object?> IEnumerable<object?>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(object? item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Remove(object? item) => throw new NotSupportedException();

        public bool Contains(object? item)
        {
            foreach (var stateItem in _collection)
            {
                if (Equals(item, stateItem.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(object?[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count => _collection.Count;
        bool ICollection.IsSynchronized => ((ICollection)_collection).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)_collection).SyncRoot;
        int ICollection<object?>.Count => _collection.Count;
        public bool IsReadOnly => true;
        int IReadOnlyCollection<object?>.Count => _collection.Count;

        public struct Enumerator : IEnumerator<object?>, IEnumerator
        {
            private Dictionary<string, StateItem>.ValueCollection.Enumerator _enumerator;

            public Enumerator(Dictionary<string, StateItem>.ValueCollection.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                ((IEnumerator)_enumerator).Reset();
            }

            public object? Current => _enumerator.Current.Value;

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }

    public void Read(ref ViewStateReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var key = reader.Read<string>();
            var value = reader.Read<object?>();

            if (value == null)
            {
                _bag.Remove(key);
            }
            else
            {
                _bag[key] = new StateItem(value, true);
            }
        }
    }

    public void Write(ref ViewStateWriter writer)
    {
        foreach (var kv in _bag)
        {
            if (!kv.Value.StoreInView) continue;
            
            writer.Write(kv.Key);
            writer.Write(kv.Value.Value);
        }
    }
}
