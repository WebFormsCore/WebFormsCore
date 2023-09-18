using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace WebFormsCore.Serializer;

public class ListViewStateSerializer : EnumerableViewStateSerializer<IList>
{
    private readonly ConcurrentDictionary<Type, Func<int, IList>> _constructors = new();

    protected override IList Create(Type typeArgument, int count, IList? defaultValue)
    {
        if (defaultValue != null && defaultValue.Count == count)
        {
            return defaultValue;
        }

        return GetListConstructor(typeArgument)(count);
    }

    protected override void Set(IList collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type typeArgument, int index, object? value)
    {
        if (index < collection.Count)
        {
            collection[index] = value!;
        }
        else if (index == collection.Count)
        {
            collection.Add(value!);
        }
        else
        {
            var defaultValue = typeArgument.IsValueType
                ? Activator.CreateInstance(typeArgument)
                : null;

            while (index > collection.Count)
            {
                collection.Add(defaultValue);
            }

            collection.Add(value!);
        }
    }

    protected override bool IsSupported(Type type, [NotNullWhen(true)] out Type? typeArgument)
    {
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(List<>))
            {
                typeArgument = type.GetGenericArguments()[0];
                return true;
            }
        }

        typeArgument = null;
        return false;
    }

    protected override int GetCount(IList? collection)
    {
        return collection?.Count ?? 0;
    }

    protected override object? GetValue(IList? collection, int index)
    {
        return collection?[index];
    }

    private Func<int, IList> GetListConstructor(Type typeArgument)
    {
        return _constructors.GetOrAdd(typeArgument, static type =>
        {
            var genericType = typeof(List<>).MakeGenericType(type);

#if NET6_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return c => (IList)Activator.CreateInstance(genericType, c)!;
            }
#endif

            var parameter = Expression.Parameter(typeof(int));
            var ctor = genericType.GetConstructor(new[] { typeof(int) });

            if (ctor != null)
            {
                return Expression.Lambda<Func<int, IList>>(Expression.New(ctor!, parameter), parameter).Compile();
            }

            ctor = genericType.GetConstructor(Type.EmptyTypes);

            if (ctor != null)
            {
                return Expression.Lambda<Func<int, IList>>(Expression.New(ctor!), parameter).Compile();
            }

            throw new InvalidOperationException($"Unable to find a constructor for {genericType}");
        });
    }

}
