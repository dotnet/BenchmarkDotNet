using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    // See http://stackoverflow.com/questions/32114308/weird-performance-increase-in-simple-benchmark
    [Config(typeof(Config))]
    public class Jit_RegistersVsStack
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyJitX86);
            }
        }

        [Params(false, true)]
        public bool CallStopwatchTimestamp { get; set; }

        [Setup]
        public void Setup()
        {
            if (CallStopwatchTimestamp)
                Stopwatch.GetTimestamp();
        }

        private const int IterationCount = 10001;

        [Benchmark(OperationsPerInvoke = IterationCount)]
        public string WithStopwatch()
        {
            double a = 1, b = 1;
            var sw = new Stopwatch();
            for (int i = 0; i < IterationCount; i++)
            {
                // fld1
                // fadd        qword ptr [ebp-0Ch]
                // fstp        qword ptr [ebp-0Ch]
                a = a + b;
            }
            return string.Format("{0}{1}", a, sw.ElapsedMilliseconds);
        }

        [Benchmark(OperationsPerInvoke = IterationCount)]
        public string WithoutStopwatch()
        {
            double a = 1, b = 1;
            for (int i = 0; i < IterationCount; i++)
            {
                // fld1
                // faddp       st(1),st
                a = a + b;
            }
            return string.Format("{0}", a);
        }
    }
}