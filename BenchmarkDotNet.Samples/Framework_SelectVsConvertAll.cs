using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Framework_SelectVsConvertAll
    {
        private const int IterationCount = 10000000;
        private readonly List<int> list = new List<int> { 1, 2, 3, 4, 5 };

        [Benchmark]
        public List<int> Select()
        {
            List<int> newList = null;
            for (int i = 0; i < IterationCount; i++)
                newList = list.Select(x => 2 * x).ToList();
            return newList;
        }

        [Benchmark]
        public List<int> ConvertAll()
        {
            List<int> newList = null;
            for (int i = 0; i < IterationCount; i++)
                newList = list.ConvertAll(x => 2 * x).ToList();
            return newList;
        }
    }
}