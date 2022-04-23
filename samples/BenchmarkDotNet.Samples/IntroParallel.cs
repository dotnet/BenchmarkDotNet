using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    public class IntroParallel
    {
        public IEnumerable<object> GetParams()
        {
            for (int i = 1; i < Environment.ProcessorCount - 1; i++)
            {
                yield return i.ToString().PadLeft(2);
            }
        }

        [ParamsSource(nameof(GetParams))]
        public string CoreId { get; set; }

        [Benchmark]
        public int Run()
        {
            int sum = 0;
            for (int i = 0; i < 100_000; i++)
            {
                sum += i;
            }
            return sum;
        }
    }
}
