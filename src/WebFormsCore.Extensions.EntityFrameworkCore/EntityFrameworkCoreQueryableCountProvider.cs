using Microsoft.EntityFrameworkCore;
using WebFormsCore.Providers;

namespace WebFormsCore;

internal class EntityFrameworkCoreQueryableCountProvider : IQueryableCountProvider
{
    public ValueTask<int> CountAsync<T>(IQueryable<T> queryable)
    {
        return new ValueTask<int>(queryable.CountAsync());
    }
}
