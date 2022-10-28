using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class PerfCollectProfilerAttribute : Attribute, IConfigSource
    {
        /// <param name="performExtraBenchmarksRun">When set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. False by default.</param>
        /// <param name="timeoutInSeconds">How long should we wait for the perfcollect script to finish processing the trace. 300s by default.</param>
        public PerfCollectProfilerAttribute(bool performExtraBenchmarksRun = false, int timeoutInSeconds = 300)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new PerfCollectProfiler(new PerfCollectProfilerConfig(performExtraBenchmarksRun, timeoutInSeconds)));
        }

        public IConfig Config { get; }
    }
}