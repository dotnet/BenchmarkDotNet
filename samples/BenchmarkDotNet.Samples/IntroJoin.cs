using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // Run BenchmarkSwitcher with arguments: "--join --category=IntroJoinA"

    [DryJob]
    [UseLocalJobOnly]
    public class IntroJoin1
    {
        [Benchmark]
        [BenchmarkCategory("IntroJoinA")]
        public void A() => Thread.Sleep(10);

        [Benchmark]
        [BenchmarkCategory("IntroJoinB")]
        public void B() => Thread.Sleep(10);
    }

    [DryJob]
    [UseLocalJobOnly]
    public class IntroJoin2
    {
        [Benchmark]
        [BenchmarkCategory("IntroJoinA")]
        public void A() => Thread.Sleep(10);

        [Benchmark]
        [BenchmarkCategory("IntroJoinB")]
        public void B() => Thread.Sleep(10);
    }
}