using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public class ForeachArrayCompetition
    {
        private readonly int[] array;

        public ForeachArrayCompetition()
        {
            array = new int[500000000];
        }

        [Benchmark]
        public int ArrayForWithoutOptimization()
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            return sum;
        }

        [Benchmark]
        public double ArrayForWithOptimization()
        {
            int length = array.Length;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += array[i];
            return sum;
        }

        [Benchmark]
        public double ArrayForeach()
        {
            int sum = 0;
            foreach (var item in array)
                sum += item;
            return sum;
        }

        [Benchmark]
        public double ArrayForEach()
        {
            int sum = 0;
            Array.ForEach(array, i => { sum += i; });
            return sum;
        }
    }
}