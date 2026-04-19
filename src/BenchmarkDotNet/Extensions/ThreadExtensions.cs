using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Extensions;

internal static class ThreadExtensions
{
    public static bool TrySetPriority(this Thread thread, ThreadPriority priority, ILogger logger)
    {
        if (!thread.IsAlive)
        {
            return false;
        }
        try
        {
            thread.Priority = priority;
            return true;
        }
        catch (Exception ex)
        {
            logger.WriteLineError($"// ! Failed to set up priority {priority} for thread {thread}. Make sure you have the right permissions. Message: {ex.Message}");
            return false;
        }
    }
}
