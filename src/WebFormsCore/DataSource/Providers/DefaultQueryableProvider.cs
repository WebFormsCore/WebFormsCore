using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebFormsCore.Providers;

internal class DefaultQueryableProvider : IQueryableProvider
{
    public ValueTask<int> CountAsync<T>(IQueryable<T> queryable)
    {
        return new ValueTask<int>(queryable.Count());
    }

    public async ValueTask<List<T>> ToListAsync<T>(IQueryable<T> queryable, int? count)
    {
        var list = count.HasValue ? new List<T>(count.Value) : [];

        if (queryable is IAsyncEnumerable<T> asyncEnumerable)
        {
            await foreach (var item in asyncEnumerable)
            {
                list.Add(item);
            }
        }
        else
        {
            list.AddRange(queryable);
        }

        return list;
    }
}
