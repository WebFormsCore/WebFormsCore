using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebFormsCore.Providers;

public interface IQueryableProvider
{
    ValueTask<int> CountAsync<T>(IQueryable<T> queryable);

    ValueTask<List<T>> ToListAsync<T>(IQueryable<T> queryable, int? count = null);
}