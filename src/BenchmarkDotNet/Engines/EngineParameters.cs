using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public static readonly IResolver DefaultResolver = new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);

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

        [PublicAPI] public string BenchmarkName { get;  set; }

        public bool NeedsJitting => TargetJob.ResolveValue(RunMode.RunStrategyCharacteristic, DefaultResolver).NeedsJitting();

        public bool HasInvocationCount => TargetJob.HasValue(RunMode.InvocationCountCharacteristic);

        public bool HasUnrollFactor => TargetJob.HasValue(RunMode.UnrollFactorCharacteristic);

        public int UnrollFactor => TargetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, DefaultResolver);

        public TimeInterval IterationTime => TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, DefaultResolver);
    }
}