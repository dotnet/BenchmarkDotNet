using BenchmarkDotNet.Attributes;
using System;

namespace BenchmarkDotNet.Samples
{
    public class IntroMemoryRandomization
    {
        [Params(512 * 4)]
        public int Size;

        private int[] array;
        private int[] destination;

        [GlobalSetup]
        public void Setup()
        {
            array = new int[Size];
            destination = new int[Size];
        }

        [Benchmark]
        [MemoryRandomization(false)]
        public void Array_RandomizationDisabled() => Array.Copy(array, destination, Size);

        [Benchmark]
        [MemoryRandomization(true)]
        [MaxIterationCount(40)] // the benchmark becomes multimodal and need a lower limit of max iterations than the default
        public void Array_RandomizationEnabled() => Array.Copy(array, destination, Size);
    }
}
