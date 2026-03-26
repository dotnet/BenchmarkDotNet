using BenchmarkDotNet.Loggers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ThreadExtensions
    {
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")] // TODO: check result
        public static bool TrySetPriority(
            this Thread thread,
            ThreadPriority priority,
            ILogger logger)
        {
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
