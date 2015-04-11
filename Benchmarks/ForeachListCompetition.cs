using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ForeachListCompetition
    {
        private readonly List<int> list = Enumerable.Range(0, 200000000).ToList();

        [Benchmark]
        public int ListForWithoutOptimization()
        {
            int sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i];
            return sum;
        }

        [Benchmark]
        public double ListForWithOptimization()
        {
            int length = list.Count;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += list[i];
            return sum;
        }

        [Benchmark]
        public double ListForeach()
        {
            int sum = 0;
            foreach (var item in list)
                sum += item;
            return sum;
        }

        [Benchmark]
        public double ListForEach()
        {
            int sum = 0;
            list.ForEach(i => { sum += i; });
            return sum;
        }
    }
}