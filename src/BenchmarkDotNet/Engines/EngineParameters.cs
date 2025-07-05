using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public static readonly IResolver DefaultResolver = new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);

        public IResolver Resolver { get; } = DefaultResolver;
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

        public FrozenEngineParameters Freeze() => new(this);
    }

    public sealed class FrozenEngineParameters(EngineParameters engineParameters)
    {
        public IResolver Resolver { get; } = engineParameters.Resolver;
        public IHost Host { get; } = engineParameters.Host;
        public Action<long> WorkloadActionNoUnroll { get; } = engineParameters.WorkloadActionNoUnroll;
        public Action<long> WorkloadActionUnroll { get; } = engineParameters.WorkloadActionUnroll;
        public Action Dummy1Action { get; } = engineParameters.Dummy1Action;
        public Action Dummy2Action { get; } = engineParameters.Dummy2Action;
        public Action Dummy3Action { get; } = engineParameters.Dummy3Action;
        public Action<long> OverheadActionNoUnroll { get; } = engineParameters.OverheadActionNoUnroll;
        public Action<long> OverheadActionUnroll { get; } = engineParameters.OverheadActionUnroll;
        public Job TargetJob { get; } = engineParameters.TargetJob;
        public long OperationsPerInvoke { get; } = engineParameters.OperationsPerInvoke;
        public Action GlobalSetupAction { get; } = engineParameters.GlobalSetupAction;
        public Action GlobalCleanupAction { get; } = engineParameters.GlobalCleanupAction;
        public Action IterationSetupAction { get; } = engineParameters.IterationSetupAction;
        public Action IterationCleanupAction { get; } = engineParameters.IterationCleanupAction;
        public bool MeasureExtraStats { get; } = engineParameters.MeasureExtraStats;
        public string BenchmarkName { get; } = engineParameters.BenchmarkName;
    }
}