using System.Linq;
using System.Threading.Tasks;

namespace WebFormsCore.Providers;

internal class DefaultQueryableCountProvider : IQueryableCountProvider
{
    public ValueTask<int> CountAsync<T>(IQueryable<T> queryable)
    {
        return new ValueTask<int>(queryable.Count());
    }
}
