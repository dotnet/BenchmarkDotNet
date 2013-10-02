using System;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ForeachArrayCompetition : BenchmarkCompetition
    {
        private int[] array;

        protected override void Prepare()
        {
            array = new int[500000000];
        }

        [BenchmarkMethod]
        public int ArrayForWithoutOptimization()
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            return sum;
        }

        [BenchmarkMethod]
        public double ArrayForWithOptimization()
        {
            int length = array.Length;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += array[i];
            return sum;
        }

        [BenchmarkMethod]
        public double ArrayForeach()
        {
            int sum = 0;
            foreach (var item in array)
                sum += item;
            return sum;
        }

        [BenchmarkMethod]
        public double ArrayForEach()
        {
            int sum = 0;
            Array.ForEach(array, i => { sum += i; });
            return sum;
        }
    }
}