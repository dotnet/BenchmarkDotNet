using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Framework
{
    [ClrJob, MonoJob]
    public class Framework_Stopwatch
    {
        [Benchmark]
        public long Latency()
        {
            return Stopwatch.GetTimestamp();
        }

        [Benchmark]
        public long Granularity()
        {
            long lastTimestamp = Stopwatch.GetTimestamp();
            while (Stopwatch.GetTimestamp() == lastTimestamp)
            {
            }
            return lastTimestamp;
        }
    }
}
