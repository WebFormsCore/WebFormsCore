using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using WebFormsCore.UI;

namespace WebFormsCore.Serializer;

public abstract class EnumerableViewStateSerializer<T>(IOptions<ViewStateOptions>? options) : IViewStateSerializer
    where T : class
{
    private readonly IOptions<ViewStateOptions> _options = options ?? Options.Create(new ViewStateOptions());

    public bool CanSerialize(Type type)
    {
        return IsSupported(type, out _);
    }

    public void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue)
    {
        var countSpan = writer.Reserve(sizeof(ushort));
        ushort count;

        if (value is null)
        {
            count = ushort.MaxValue;
            MemoryMarshal.Write(writer.GetUnsafeSpan(countSpan), ref count);
            return;
        }

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        count = 0;

        foreach (var item in (IEnumerable)value)
        {
            writer.WriteObject(typeArgument, item);
            count++;
        }

        if (count > _options.Value.MaxCollectionLength)
        {
            throw new ViewStateException("Collection size is too large");
        }

        MemoryMarshal.Write(writer.GetUnsafeSpan(countSpan), ref count);
    }

    public object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, object? defaultCollectionObject)
    {
        var countSpan = reader.ReadBytes(sizeof(ushort));
        var count = MemoryMarshal.Read<ushort>(countSpan);

        if (count == ushort.MaxValue)
        {
            return null;
        }

        if (count > _options.Value.MaxCollectionLength)
        {
            throw new ViewStateException("Collection size is too large");
        }

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        var defaultCollection = defaultCollectionObject as T;
        var defaultCollectionCount = GetCount(defaultCollection);
        var collection = Create(typeArgument, count, (T?)defaultCollectionObject);

        for (var i = 0; i < count; i++)
        {
            var defaultValue = i < defaultCollectionCount ? GetValue(defaultCollection, i) : null;
            var value = reader.ReadObject(typeArgument, defaultValue);

            Set(collection, typeArgument, i, value);
        }

        return collection;
    }

    public bool StoreInViewState(Type type, object? value, object? defaultValue) => true;

    public void TrackViewState(Type type, object? value, ViewStateProvider provider)
    {
        if (value is null)
        {
            return;
        }

        if (!IsSupported(type, out var itemType))
        {
            throw new InvalidOperationException("Invalid type");
        }

        foreach (var item in (IEnumerable)value)
        {
            if (item is IViewStateObject viewStateObject)
            {
                provider.TrackViewState(itemType, viewStateObject);
            }
        }
    }

    protected abstract T Create(Type typeArgument, int count, T? defaultValue);

    protected abstract void Set(T collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type typeArgument, int index, object? value);

    protected abstract bool IsSupported(Type type, [NotNullWhen(true)] out Type? typeArgument);

    protected abstract int GetCount(T? collection);

    protected abstract object? GetValue(T? collection, int index);
}
