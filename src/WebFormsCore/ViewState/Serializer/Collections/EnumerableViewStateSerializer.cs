using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WebFormsCore.Serializer;

public abstract class EnumerableViewStateSerializer<T> : IViewStateSerializer
    where T : class
{
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
            MemoryMarshal.Write(writer.GetSpan(countSpan), ref count);
            return;
        }

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        count = 0;

        foreach (var item in (IEnumerable)value)
        {
            writer.Write(typeArgument, item, null);
            count++;
        }

        MemoryMarshal.Write(writer.GetSpan(countSpan), ref count);
    }

    public object? Read(Type type, ref ViewStateReader reader, object? defaultValue)
    {
        var countSpan = reader.ReadBytes(sizeof(ushort));
        var count = MemoryMarshal.Read<ushort>(countSpan);

        if (count == ushort.MaxValue)
        {
            return null;
        }

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        var collection = Create(typeArgument, count, (T?)defaultValue);

        for (var i = 0; i < count; i++)
        {
            var value = reader.Read(typeArgument, null);

            Add(collection, i, value);
        }

        return collection;
    }

    public bool StoreInViewState(Type type, object? value, object? defaultValue) => true;

    protected abstract T Create(Type typeArgument, int count, T? defaultValue);

    protected abstract void Add(T collection, int index, object? value);

    protected abstract bool IsSupported(Type type, [NotNullWhen(true)] out Type? typeArgument);
}
