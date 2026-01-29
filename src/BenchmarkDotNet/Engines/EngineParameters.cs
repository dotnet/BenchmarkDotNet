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
        public required IHost Host { get; set; }
        public required Action<long> WorkloadActionNoUnroll { get; set; }
        public required Action<long> WorkloadActionUnroll { get; set; }
        public required Action<long> OverheadActionNoUnroll { get; set; }
        public required Action<long> OverheadActionUnroll { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public required Action GlobalSetupAction { get; set; }
        public required Action GlobalCleanupAction { get; set; }
        public required Action IterationSetupAction { get; set; }
        public required Action IterationCleanupAction { get; set; }
        public bool RunExtraIteration { get; set; }
        public required string BenchmarkName { get;  set; }
        public required Diagnosers.CompositeInProcessDiagnoserHandler InProcessDiagnoserHandler { get; set; }
    }
}