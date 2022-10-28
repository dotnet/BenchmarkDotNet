using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 0,
        warmupCount: 0, iterationCount: 1)]
    public class IntroSetupCleanupTarget
    {
        [GlobalSetup(Target = nameof(BenchmarkA))]
        public void GlobalSetupA()
            => Console.WriteLine("// " + "GlobalSetup A");

        [Benchmark]
        public void BenchmarkA()
            => Console.WriteLine("// " + "Benchmark A");

        [GlobalSetup(Targets = new[] { nameof(BenchmarkB), nameof(BenchmarkC) })]
        public void GlobalSetupB()
            => Console.WriteLine("// " + "GlobalSetup B");

        [Benchmark]
        public void BenchmarkB()
            => Console.WriteLine("// " + "Benchmark B");

        [Benchmark]
        public void BenchmarkC()
            => Console.WriteLine("// " + "Benchmark C");

        [Benchmark]
        public void BenchmarkD()
            => Console.WriteLine("// " + "Benchmark D");
    }
}