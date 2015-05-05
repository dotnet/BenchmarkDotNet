using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Framework_ForeachList
    {
        private const int IterationCount = 10001, ListSize = 50001;
        private readonly List<int> list = Enumerable.Range(0, ListSize).ToList();

        [Benchmark]
        public int For()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < list.Count; i++)
                    sum += list[i];
            return sum;
        }

        [Benchmark]
        public double Foreach()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                foreach (var item in list)
                    sum += item;
            return sum;
        }

        [Benchmark]
        public double ListForEach()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                list.ForEach(i => { sum += i; });
            return sum;
        }
    }
}