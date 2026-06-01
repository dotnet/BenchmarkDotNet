using System.Reflection;
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

        /// <summary>
        /// The benchmark method, used by the jit stage to watch for its tier-up via JIT events.
        /// </summary>
        public required MethodInfo WorkloadMethod { get; set; }

        /// <summary>
        /// Whether the jit stage may watch JIT tier-up events. Disabled by the stage-enumeration unit
        /// tests, which drive the stage with mock (non-executing) workloads that never raise events.
        /// </summary>
        internal bool EnableJitListener { get; set; } = true;

        public long OperationsPerInvoke { get; set; } = 1;
        public required Func<ValueTask> GlobalSetupAction { get; set; }
        public required Func<ValueTask> GlobalCleanupAction { get; set; }
        public required Func<ValueTask> IterationSetupAction { get; set; }
        public required Func<ValueTask> IterationCleanupAction { get; set; }
        public bool RunExtraIteration { get; set; }
        public required string BenchmarkName { get; set; }
        public required Diagnosers.CompositeInProcessDiagnoserHandler InProcessDiagnoserHandler { get; set; }
    }
}