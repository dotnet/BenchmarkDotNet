using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public class Framework_ForeachArray
    {
        private const int IterationCount = 10001, ArraySize = 50001;
        private readonly int[] array = new int[ArraySize];

        [Benchmark]
        public double For()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < array.Length; i++)
                    sum += array[i];
            return sum;
        }

        [Benchmark]
        public double Foreach()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                foreach (var item in array)
                    sum += item;
            return sum;
        }

        [Benchmark]
        public double ArrayForEach()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                Array.ForEach(array, i => { sum += i; });
            return sum;
        }
    }
}