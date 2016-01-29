using System;
using System.Diagnostics;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ProcessExtensions
    {
        // no catch blocks on purpose -> if this fails, the measurements might be inaccurate

        public static void EnsureHighPriority(this Process process)
        {
            process.PriorityClass = ProcessPriorityClass.High;;
        }

        public static void EnsureRightProcessorAffinity(this Process process)
        {
            process.ProcessorAffinity = new IntPtr(2);
        }
    }
}