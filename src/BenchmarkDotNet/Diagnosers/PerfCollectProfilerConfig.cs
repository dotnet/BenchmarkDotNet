using System;

namespace BenchmarkDotNet.Diagnosers
{
    public class PerfCollectProfilerConfig
    {
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        /// <param name="timeoutInSeconds">how long should we wait for the perfcollect script to start collecting and/or finish processing the trace. 30s by default</param>
        public PerfCollectProfilerConfig(bool performExtraBenchmarksRun = true, int timeoutInSeconds = 60)
        {
            RunMode = performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            Timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        }

        public TimeSpan Timeout { get; }

        public RunMode RunMode { get; }
    }
}
