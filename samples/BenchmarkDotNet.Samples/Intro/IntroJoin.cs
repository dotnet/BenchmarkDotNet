using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    // Run BenchmarkSwither with arguments: "* --join --category=IntroJoinA"
    
    [DryJob]
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