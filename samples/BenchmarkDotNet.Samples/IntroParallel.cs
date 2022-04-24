using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

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
        public string Id { get; set; }

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
