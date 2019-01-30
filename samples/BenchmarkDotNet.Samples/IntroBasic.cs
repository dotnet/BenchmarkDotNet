using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // It is very easy to use BenchmarkDotNet. You should just create a class
    public class IntroBasic
    {
        // And define a method with the Benchmark attribute
        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        // You can write a description for your method.
        [Benchmark(Description = "Thread.Sleep(10)")]
        public void SleepWithDescription() => Thread.Sleep(10);
    }
}
