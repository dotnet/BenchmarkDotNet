using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // It is very easy to use BenchmarkDotNet. You should just create a class
    [DryJob]
    public class IntroCustomEnvironmentInfo
    {
        [CustomEnvironmentInfo]
        public static string CustomLine() => "Single custom line";

        [CustomEnvironmentInfo]
        public static IEnumerable<string> SequenceOfCustomLines()
        {
            yield return "First custom line";
            yield return "Second custom line";
        }

        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        [Benchmark(Description = "Thread.Sleep(10)")]
        public void SleepWithDescription() => Thread.Sleep(10);
    }
}
