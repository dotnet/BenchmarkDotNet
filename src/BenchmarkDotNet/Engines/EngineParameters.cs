using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public static readonly IResolver DefaultResolver = new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);

        public IResolver Resolver { get; set; } = DefaultResolver;
        public IHost Host { get; set; }
        public Action<long> WorkloadActionNoUnroll { get; set; }
        public Action<long> WorkloadActionUnroll { get; set; }
        public Action Dummy1Action { get; set; }
        public Action Dummy2Action { get; set; }
        public Action Dummy3Action { get; set; }
        public Action<long> OverheadActionNoUnroll { get; set; }
        public Action<long> OverheadActionUnroll { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public Action GlobalSetupAction { get; set; }
        public Action GlobalCleanupAction { get; set; }
        public Action IterationSetupAction { get; set; }
        public Action IterationCleanupAction { get; set; }
        public bool MeasureExtraStats { get; set; }
        public string BenchmarkName { get;  set; }
    }
}