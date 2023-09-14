using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.Serializer;

public class ArrayViewStateSerializer : EnumerableViewStateSerializer<Array>
{
    private readonly ConcurrentDictionary<Type, Array> _empty = new();

    protected override Array Create(Type typeArgument, int count, Array? defaultValue)
    {
        if (defaultValue != null && defaultValue.Length == count)
        {
            return defaultValue;
        }

        return count == 0
            ? GetEmptyArray(typeArgument)
            : Array.CreateInstance(typeArgument, count);
    }

    protected override void Set(Array collection, int index, object? value)
    {
        collection.SetValue(value, index);
    }

    private Array GetEmptyArray(Type typeArgument)
    {
        return _empty.GetOrAdd(typeArgument, static type =>
        {
            var empty = typeof(Array)
                .GetMethod(nameof(Array.Empty))
                ?.MakeGenericMethod(type);

            var result = empty is not null
                ? empty.Invoke(null, null)!
                : Array.CreateInstance(type, 0);

            return (Array) result;
        });
    }

    protected override bool IsSupported(Type type, [NotNullWhen(true)] out Type? typeArgument)
    {
        if (type.IsArray)
        {
            typeArgument = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(IEnumerable<>) ||
                definition == typeof(IList<>) ||
                definition == typeof(ICollection<>) ||
                definition == typeof(IReadOnlyCollection<>) ||
                definition == typeof(IReadOnlyList<>))
            {
                typeArgument = type.GetGenericArguments()[0];
                return true;
            }
        }

        typeArgument = null;
        return false;
    }

    protected override int GetCount(Array? collection)
    {
        return collection?.Length ?? 0;
    }

    protected override object? GetValue(Array? collection, int index)
    {
        return collection?.GetValue(index);
    }
}
