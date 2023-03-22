using System.Collections;

namespace WebFormsCore.Implementation;

public class FeatureCollectionImpl : IFeatureCollection
{
    private Microsoft.AspNetCore.Http.Features.IFeatureCollection _collection = null!;

    public void SetFeatureCollection(Microsoft.AspNetCore.Http.Features.IFeatureCollection collection)
    {
        _collection = collection;
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool IsReadOnly => _collection.IsReadOnly;
    public int Revision => _collection.Revision;

    public object? this[Type key]
    {
        get => _collection[key];
        set => _collection[key] = value;
    }

    public TFeature? Get<TFeature>() => _collection.Get<TFeature>();

    public void Set<TFeature>(TFeature? instance) => _collection.Set(instance);
}
