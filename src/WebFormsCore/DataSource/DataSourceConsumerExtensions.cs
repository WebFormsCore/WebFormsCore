using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace WebFormsCore;

public static class DataSourceConsumerExtensions
{
    public static void SetDataSource<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IQueryable<T> source)
    {
        consumer.DataSource = new DataSource<T>(source);
    }

    public static void SetDataSource<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IEnumerable<T> source)
    {
        consumer.DataSource = new DataSource<T>(source);
    }

    public static void SetDataSource<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IAsyncEnumerable<T> source)
    {
        consumer.DataSource = new DataSource<T>(source);
    }

    public static ValueTask LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IQueryable<T> source)
    {
        SetDataSource(consumer, source);
        return consumer.DataBindAsync();
    }

    public static ValueTask LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IEnumerable<T> source)
    {
        SetDataSource(consumer, source);
        return consumer.DataBindAsync();
    }

    public static ValueTask LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this IDataSourceConsumer consumer, IAsyncEnumerable<T> source)
    {
        SetDataSource(consumer, source);
        return consumer.DataBindAsync();
    }

}
