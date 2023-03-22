using System;
using System.Collections;
using System.Collections.Generic;

namespace WebFormsCore;

public class FeatureCollection : IFeatureCollection
{
    private readonly IDictionary<Type, object> _features = new Dictionary<Type, object>();

    public bool IsReadOnly => false;

    public int Revision { get; private set; }

    public object? this[Type key]
    {
        get => _features[key];
        set
        {
            if (value == null)
            {
                _features.Remove(key);
            }
            else
            {
                _features[key] = value;
            }

            Revision++;
        }
    }

    public TFeature? Get<TFeature>()
    {
        return _features.TryGetValue(typeof(TFeature), out var obj) && obj is TFeature value
            ? value
            : default;
    }

    public void Set<TFeature>(TFeature? instance)
    {
        this[typeof(TFeature)] = instance;
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        return _features.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Reset()
    {
        _features.Clear();
        Revision = 0;
    }
}
