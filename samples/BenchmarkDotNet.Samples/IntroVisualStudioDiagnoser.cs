using System;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace BenchmarkDotNet.Samples
{
    // Enables profiling with the CPU Usage tool
    // See: https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
    [CPUUsageDiagnoser]
    public class IntroVisualStudioProfiler
    {
        private readonly Random rand = new Random(42);

        [Benchmark]
        public void BurnCPU()
        {
            for (int i = 0; i < 100000; ++i)
            {
                rand.Next(1, 100);
            }
        }
    }
}