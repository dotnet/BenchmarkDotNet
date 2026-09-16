#if !NET10_0_OR_GREATER
namespace System.Linq;

// System.Linq.AsyncEnumerable ships in the shared framework from .NET 10. BenchmarkDotNet provides the two members it
// uses rather than taking the package, because the rest of that surface captures the ambient SynchronizationContext,
// which BenchmarkDotNet must never do while its pump owns the calling thread. See CompositeValidator.ValidateAsync.
internal static class AsyncEnumerable
{
    public static IAsyncEnumerable<TSource> Empty<TSource>()
        => EmptyAsyncEnumerable<TSource>.Instance;

    public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return FromIterator(source);

        static async IAsyncEnumerable<TSource> FromIterator(IEnumerable<TSource> source)
        {
            foreach (TSource element in source)
            {
                yield return element;
            }
        }
    }

    private sealed class EmptyAsyncEnumerable<TSource> : IAsyncEnumerable<TSource>, IAsyncEnumerator<TSource>
    {
        public static readonly EmptyAsyncEnumerable<TSource> Instance = new();

        public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

        public TSource Current => default!;

        public ValueTask<bool> MoveNextAsync() => new(false);

        public ValueTask DisposeAsync() => default;
    }
}
#endif
