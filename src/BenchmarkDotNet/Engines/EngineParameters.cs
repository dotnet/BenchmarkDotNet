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
        public IHost Host { get; set; }
        public Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionNoUnroll { get; set; }
        public Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionUnroll { get; set; }
        public Func<long, IClock, ValueTask<ClockSpan>> OverheadActionNoUnroll { get; set; }
        public Func<long, IClock, ValueTask<ClockSpan>> OverheadActionUnroll { get; set; }
        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public Func<ValueTask> GlobalSetupAction { get; set; }
        public Func<ValueTask> GlobalCleanupAction { get; set; }
        public Func<ValueTask> IterationSetupAction { get; set; }
        public Func<ValueTask> IterationCleanupAction { get; set; }
        public bool RunExtraIteration { get; set; }
        public string BenchmarkName { get;  set; }
        public Diagnosers.CompositeInProcessDiagnoserHandler InProcessDiagnoserHandler { get; set; }
    }
}