using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class EtwProfilerAttribute : Attribute, IConfigSource
    {
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed on more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        /// <param name="bufferSizeInMb">ETW session buffer size, in MB. 256 by default</param>
        public EtwProfilerAttribute(bool performExtraBenchmarksRun = true, int bufferSizeInMb = 256)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new EtwProfiler(new EtwProfilerConfig(performExtraBenchmarksRun, bufferSizeInMb)));
        }

        public IConfig Config { get; }
    }
}