using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public class DataSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IDataSource
{
    public DataSource(object value)
    {
        Value = value;
    }

    public object Value { get; }

    public Type ElementType => typeof(T);

    public ValueTask LoadAsync(IDataSourceConsumer consumer)
    {
        return consumer.LoadDataSourceAsync<T>(Value);
    }
}
