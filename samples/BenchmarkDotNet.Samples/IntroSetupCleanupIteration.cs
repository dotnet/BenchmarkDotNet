using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1,
        warmupCount: 2, iterationCount: 3)]
    public class IntroSetupCleanupIteration
    {
        private int setupCounter;
        private int cleanupCounter;

        [IterationSetup]
        public void IterationSetup()
            => Console.WriteLine($"// IterationSetup ({++setupCounter})");

        [IterationCleanup]
        public void IterationCleanup()
            => Console.WriteLine($"// IterationCleanup ({++cleanupCounter})");

        [GlobalSetup]
        public void GlobalSetup()
            => Console.WriteLine("// " + "GlobalSetup");

        [GlobalCleanup]
        public void GlobalCleanup()
            => Console.WriteLine("// " + "GlobalCleanup");

        [Benchmark]
        public void Benchmark()
            => Console.WriteLine("// " + "Benchmark");
    }
}