using Microsoft.EntityFrameworkCore;
using WebFormsCore.Providers;

namespace WebFormsCore;

internal class EntityFrameworkCoreQueryableProvider : IQueryableProvider
{
    public ValueTask<int> CountAsync<T>(IQueryable<T> queryable)
    {
        return new ValueTask<int>(queryable.CountAsync());
    }

    public ValueTask<List<T>> ToListAsync<T>(IQueryable<T> queryable, int? count)
    {
        return new ValueTask<List<T>>(queryable.ToListAsync());
    }
}
