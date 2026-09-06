#if !NET10_0_OR_GREATER
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq;

// System.Linq.AsyncEnumerable ships in the shared framework from .NET 10, and BenchmarkDotNet polyfills the two
// members it needs rather than taking the package - see Extensions/Polyfills/AsyncEnumerable.cs for why. The tests
// see those two through InternalsVisibleTo; ToArrayAsync is theirs alone, so it lives here. A separate class, not
// more members on AsyncEnumerable: that name is already taken by the one they inherit.
internal static class AsyncEnumerableExtensions
{
    internal static async ValueTask<TSource[]> ToArrayAsync<TSource>(
        this IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken = default)
    {
        List<TSource> items = [];

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items.ToArray();
    }

    internal static async IAsyncEnumerable<TResult> Select<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        await foreach (var item in source.ConfigureAwait(false))
        {
            yield return selector(item);
        }
    }
}
#endif
