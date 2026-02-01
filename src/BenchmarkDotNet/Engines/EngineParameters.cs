using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    public class EngineParameters
    {
        public static readonly IResolver DefaultResolver = new CompositeResolver(BenchmarkRunnerClean.DefaultResolver, EngineResolver.Instance);

        public IResolver Resolver { get; set; } = DefaultResolver;
        public required IHost Host { get; set; }
        public required Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionNoUnroll { get; set; }
        public required Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionUnroll { get; set; }
        public required Func<long, IClock, ValueTask<ClockSpan>> OverheadActionNoUnroll { get; set; }
        public required Func<long, IClock, ValueTask<ClockSpan>> OverheadActionUnroll { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public required Func<ValueTask> GlobalSetupAction { get; set; }
        public required Func<ValueTask> GlobalCleanupAction { get; set; }
        public required Func<ValueTask> IterationSetupAction { get; set; }
        public required Func<ValueTask> IterationCleanupAction { get; set; }
        public bool RunExtraIteration { get; set; }
        public required string BenchmarkName { get;  set; }
        public required Diagnosers.CompositeInProcessDiagnoserHandler InProcessDiagnoserHandler { get; set; }
    }
}