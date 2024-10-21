using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebFormsCore;

[RequiresDynamicCode("The element type of the data source is not known at compile time. Use DataSource<T> instead.")]
public class DataSource : IDataSource
{
    private Type? _elementType;

    public DataSource(object dataSource)
    {
        Value = dataSource;
    }

    public object Value { get; }

    public Type ElementType
    {
        get
        {
            if (_elementType != null)
            {
                return _elementType;
            }

            var type = Value.GetType();

#pragma warning disable IL2075
            var elementType = type.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i =>
                {
                    var genericType = i.GetGenericTypeDefinition();

                    return genericType == typeof(IQueryable<>) ||
                           genericType == typeof(IAsyncEnumerable<>) ||
                           genericType == typeof(IEnumerable<>);
                })
                ?.GetGenericArguments()[0];

#pragma warning restore IL2075

            _elementType = elementType ?? throw new InvalidOperationException($"The type {type.FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.");

            return elementType;
        }
    }

    public ValueTask LoadAsync(IDataSourceConsumer consumer, bool dataBinding, bool filterByKeys)
    {
#pragma warning disable IL2075
#pragma warning disable IL2076
        var genericMethod = typeof(IDataSourceConsumer)
            .GetMethod(nameof(IDataSourceConsumer.LoadDataSourceAsync), BindingFlags.Instance | BindingFlags.Public)!
            .MakeGenericMethod(ElementType);
#pragma warning restore IL2075
#pragma warning restore IL2076

        return (ValueTask) genericMethod.Invoke(consumer, [Value, dataBinding, filterByKeys])!;
    }
}