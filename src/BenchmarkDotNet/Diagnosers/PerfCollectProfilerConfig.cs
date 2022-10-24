using System;

namespace BenchmarkDotNet.Diagnosers
{
    public class PerfCollectProfilerConfig
    {
        /// <param name="performExtraBenchmarksRun">When set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. False by default.</param>
        /// <param name="timeoutInSeconds">How long should we wait for the perfcollect script to finish processing the trace. 300s by default.</param>
        public PerfCollectProfilerConfig(bool performExtraBenchmarksRun = false, int timeoutInSeconds = 300)
        {
            RunMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            Timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        }

        public TimeSpan Timeout { get; }

        public RunMode RunMode { get; }
    }
}
