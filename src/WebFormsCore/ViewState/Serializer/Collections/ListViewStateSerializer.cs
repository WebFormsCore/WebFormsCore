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
        if (defaultValue == null)
        {
            return GetListConstructor(typeArgument)(count);
        }

        defaultValue.Clear();
        return defaultValue;

    }

    protected override void Add(IList collection, int index, object? value)
    {
        collection.Add(value);
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

    private Func<int, IList> GetListConstructor(Type typeArgument)
    {
        return _constructors.GetOrAdd(typeArgument, static type =>
        {
            var genericType = typeof(List<>).MakeGenericType(type);

#if NET6_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return _ => (IList)Activator.CreateInstance(genericType)!;
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
