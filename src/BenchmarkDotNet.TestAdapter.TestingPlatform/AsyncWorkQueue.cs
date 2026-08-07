using System.Collections.Concurrent;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// An ordered queue of asynchronous work items that are produced synchronously and consumed asynchronously.
    /// </summary>
    /// <remarks>
    /// BenchmarkDotNet reports its progress through synchronous callbacks (EventProcessor and ILogger) while the
    /// platform message bus and output device are asynchronous. Blocking on those from inside a callback risks
    /// deadlocking against the synchronization context BenchmarkDotNet installs while it runs, so the callbacks
    /// enqueue instead and the caller of DrainAsync does the awaiting.
    /// </remarks>
    internal sealed class AsyncWorkQueue : IDisposable
    {
        private readonly ConcurrentQueue<Func<Task>> queue = new();
        private readonly SemaphoreSlim available = new(0);
        private volatile bool completed;

        /// <summary>
        /// Queues a work item. Safe to call from any thread.
        /// </summary>
        /// <param name="work">The work to perform.</param>
        public void Enqueue(Func<Task> work)
        {
            queue.Enqueue(work);
            available.Release();
        }

        /// <summary>
        /// Signals that no more work items will be enqueued, which lets DrainAsync return once the already queued
        /// items have been processed.
        /// </summary>
        public void Complete()
        {
            completed = true;
            available.Release();
        }

        /// <summary>
        /// Processes queued work items in order until <see cref="Complete"/> has been called and the queue is empty.
        /// </summary>
        /// <returns>A task that completes when the queue has been drained.</returns>
        public async Task DrainAsync()
        {
            while (true)
            {
                await available.WaitAsync().ConfigureAwait(false);

                // Draining everything on each wake-up means a lost or surplus permit can never strand a work item.
                while (queue.TryDequeue(out var work))
                    await work().ConfigureAwait(false);

                if (completed && queue.IsEmpty)
                    return;
            }
        }

        public void Dispose() => available.Dispose();
    }
}
