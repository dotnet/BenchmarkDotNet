using System.Runtime.ExceptionServices;

namespace BenchmarkDotNet.Helpers;

internal static class DisposeHelper
{
    public static async ValueTask DisposeAllAsync(this IEnumerable<IAsyncDisposable> asyncDisposables)
    {
        List<Exception>? exceptions = null;
        foreach (var asyncDisposable in asyncDisposables)
        {
            try
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait();
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }
        switch (exceptions)
        {
            case [var exception]:
                ExceptionDispatchInfo.Capture(exception).Throw();
                break; // unreachable
            case not null:
                throw new AggregateException(exceptions);
        }
    }
}
