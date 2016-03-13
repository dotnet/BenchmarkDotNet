using System;
using System.Diagnostics;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ProcessExtensions
    {
        public static void EnsureHighPriority(this Process process, ILogger logger)
        {
            try
            {
                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Failed to set up high priority. Make sure you have the right permissions. Message: {ex.Message}");
            }
        }

        public static void EnsureProcessorAffinity(this Process process, int value)
        {
            process.ProcessorAffinity = new IntPtr(value);
        }
    }
}