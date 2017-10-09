#if !NETCOREAPP1_1

using System;
using System.Threading;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ThreadExtensions
    {
        public static bool TrySetPriority(
            this Thread thread,
            ThreadPriority priority,
            ILogger logger)
        {
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                thread.Priority = priority;
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up priority {priority} for thread {thread}. Make sure you have the right permissions. Message: {ex.Message}");
            }

            return false;

        }
    }
}
#endif