using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Framework_SelectVsConvertAll
    {
        private readonly List<int> list = new List<int> { 1, 2, 3, 4, 5 };

        [Benchmark]
        public List<int> Select()
        {
            return list.Select(x => 2 * x).ToList();
        }

        [Benchmark]
        public List<int> ConvertAll()
        {
            return list.ConvertAll(x => 2 * x).ToList();
        }
    }
}