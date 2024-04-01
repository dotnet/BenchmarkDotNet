using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotMemory;
using System.Collections.Generic;

namespace BenchmarkDotNet.Samples
{
    // Enables dotMemory profiling for all jobs
    [DotMemoryDiagnoser]
    // Adds the default "external-process" job
    // Profiling is performed using dotMemory Command-Line Profiler
    // See: https://www.jetbrains.com/help/dotmemory/Working_with_dotMemory_Command-Line_Profiler.html
    [SimpleJob]
    // Adds an "in-process" job
    // Profiling is performed using dotMemory SelfApi
    // NuGet reference: https://www.nuget.org/packages/JetBrains.Profiler.SelfApi
    [InProcess]
    public class IntroDotMemoryDiagnoser
    {
        [Params(1024)]
        public int Size;

        private byte[] dataArray;
        private IEnumerable<byte> dataEnumerable;

        [GlobalSetup]
        public void Setup()
        {
            dataArray = new byte[Size];
            dataEnumerable = dataArray;
        }

        [Benchmark]
        public int IterateArray()
        {
            var count = 0;
            foreach (var _ in dataArray)
                count++;

            return count;
        }

        [Benchmark]
        public int IterateEnumerable()
        {
            var count = 0;
            foreach (var _ in dataEnumerable)
                count++;

            return count;
        }
    }
}