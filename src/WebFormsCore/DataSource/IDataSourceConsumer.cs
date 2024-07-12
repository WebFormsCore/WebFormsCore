using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IDataSourceConsumer
{
    IDataSource? DataSource { get; set; }

    ValueTask DataBindAsync(CancellationToken token = default);

    ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object source);
}
