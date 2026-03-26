namespace BenchmarkDotNet.Extensions;

internal static class SemaphoreSlimExtensions
{
    public static async ValueTask<SemaphoreScope> EnterScopeAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new SemaphoreScope(semaphore);
    }

    internal readonly struct SemaphoreScope(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
