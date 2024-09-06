using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.Internal;

public static class EnumerableExtensions
{
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        return new AsyncEnumerable<T>(enumerable);
    }

    private class AsyncEnumerable<T>(IEnumerable<T> enumerable) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return new AsyncEnumerator<T>(enumerable.GetEnumerator());
        }
    }

    private class AsyncEnumerator<T>(IEnumerator<T> enumerator) : IAsyncEnumerator<T>
    {

        public T Current => enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(enumerator.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            enumerator.Dispose();
            return new ValueTask();
        }
    }
}
