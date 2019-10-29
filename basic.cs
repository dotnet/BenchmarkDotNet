using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
 
    public class basic
    {
        
        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        [Benchmark(Description = "Thread.Sleep(12)")]
        public void SleepWithDescription() => Thread.Sleep(12);
    }
}
