using System.Threading.Tasks;

namespace WebFormsCore;

public interface IDataSourceConsumer
{
    IDataSource? DataSource { get; set; }

    ValueTask DataBindAsync();

    ValueTask LoadDataSourceAsync<T>(object source);
}
