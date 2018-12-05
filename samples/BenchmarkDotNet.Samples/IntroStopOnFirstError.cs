using System;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [StopOnFirstError]
    public class IntroStopOnFirstError
    {
        [Benchmark(Baseline = true)]
        public int FirstMethod() => throw new Exception("Example exception.");

        [Benchmark]
        public int SecondMethod() => 1;
    }
}