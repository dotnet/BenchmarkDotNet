using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotMemory;
using System.Collections.Generic;

namespace BenchmarkDotNet.Samples
{
    // Profile benchmarks via dotMemory SelfApi profiling for all jobs
    [DotMemoryDiagnoser]
    [SimpleJob] // external-process execution
    [InProcess] // in-process execution
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