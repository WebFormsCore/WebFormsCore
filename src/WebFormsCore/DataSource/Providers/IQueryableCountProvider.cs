using System.Linq;
using System.Threading.Tasks;

namespace WebFormsCore.Providers;

public interface IQueryableCountProvider
{
    ValueTask<int> CountAsync<T>(IQueryable<T> queryable);
}