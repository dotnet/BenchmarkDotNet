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

        [Benchmark(Description = "Your description here")]
        public void YourBenchmark()
        {
            // Implement your benchmark here

            
        }
    }
}
