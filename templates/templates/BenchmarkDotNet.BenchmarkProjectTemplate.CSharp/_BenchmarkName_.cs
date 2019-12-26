using System;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace _BenchmarkProjectName_
{
#if(config)
    [Config(typeof(BenchmarkConfig))]
#endif
    public class $(BenchmarkName)
    {
        [Benchmark]
        public void Scenario1()
        {
            // Implement your benchmark here
        }

        [Benchmark]
        public void Scenario2()
        {
            // Implement your benchmark here
        }
    }
}
