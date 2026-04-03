using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventPipeProfilerAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="profile">A named pre-defined set of provider configurations that allows common tracing scenarios to be specified succinctly.</param>
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead.</param>
        public EventPipeProfilerAttribute(EventPipeProfile profile, bool performExtraBenchmarksRun = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new EventPipeProfiler(profile, performExtraBenchmarksRun: performExtraBenchmarksRun));
        }
    }
}