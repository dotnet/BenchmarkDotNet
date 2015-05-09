using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Samples
{
    public class Framework_SelectVsConvertAll
    {
        private readonly List<int> list = new List<int> { 1, 2, 3, 4, 5 };

        [Benchmark]
        [OperationsPerInvoke(5)]
        public List<int> Select()
        {
            return list.Select(x => 2 * x).ToList();
        }

        [OperationsPerInvoke(5)]
        [Benchmark]
        public List<int> ConvertAll()
        {
            return list.ConvertAll(x => 2 * x).ToList();
        }
    }
}