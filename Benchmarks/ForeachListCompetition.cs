using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ForeachListCompetition : BenchmarkCompetition
    {
        private List<int> list;

        protected override void Prepare()
        {
            list = Enumerable.Range(0, 200000000).ToList();
        }

        [BenchmarkMethod]
        public int ListForWithoutOptimization()
        {
            int sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i];
            return sum;
        }

        [BenchmarkMethod]
        public double ListForWithOptimization()
        {
            int length = list.Count;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += list[i];
            return sum;
        }

        [BenchmarkMethod]
        public double ListForeach()
        {
            int sum = 0;
            foreach (var item in list)
                sum += item;
            return sum;
        }

        [BenchmarkMethod]
        public double ListForEach()
        {
            int sum = 0;
            list.ForEach(i => { sum += i; });
            return sum;
        }
    }
}