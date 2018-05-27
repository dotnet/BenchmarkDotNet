using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public static readonly IResolver DefaultResolver = new CompositeResolver(BenchmarkRunner.DefaultResolver, EngineResolver.Instance);
        
        public IHost Host { get; set; }
        public Action<long> MainSingleAction { get; set; }
        public Action<long> MainMultiAction { get; set; }
        public Action Dummy1Action { get; set; }
        public Action Dummy2Action { get; set; }
        public Action Dummy3Action { get; set; }
        public Action<long> IdleSingleAction { get; set; }
        public Action<long> IdleMultiAction { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public Action GlobalSetupAction { get; set; }
        public Action GlobalCleanupAction { get; set; }
        public Action IterationSetupAction { get; set; }
        public Action IterationCleanupAction { get; set; }
        public bool MeasureGcStats { get; set; }
        
        public bool NeedsJitting => TargetJob.ResolveValue(RunMode.RunStrategyCharacteristic, DefaultResolver).NeedsJitting();

        public bool HasInvocationCount => TargetJob.HasValue(RunMode.InvocationCountCharacteristic);
        
        public bool HasUnrollFactor => TargetJob.HasValue(RunMode.UnrollFactorCharacteristic);

        public int UnrollFactor => TargetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, DefaultResolver);
        
        public TimeInterval IterationTime => TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, DefaultResolver);
    }
}