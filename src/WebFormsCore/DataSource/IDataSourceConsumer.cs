using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IDataSourceConsumer
{
    IDataSource? DataSource { get; set; }

    ValueTask DataBindAsync();

    ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object source);
}
