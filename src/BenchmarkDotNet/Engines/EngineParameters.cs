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
        public required string BenchmarkName { get; set; }
        public long OperationsPerInvoke { get; set; } = 1;
        public bool RunExtraIteration { get; set; }

        // The generated code fills in the properties as it proceeds.
        public Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionNoUnroll { get; set; } = null!;
        public Func<long, IClock, ValueTask<ClockSpan>> WorkloadActionUnroll { get; set; } = null!;
        public Func<long, IClock, ValueTask<ClockSpan>> OverheadActionNoUnroll { get; set; } = null!;
        public Func<long, IClock, ValueTask<ClockSpan>> OverheadActionUnroll { get; set; } = null!;

        public Func<ValueTask> GlobalSetupAction { get; set; } = null!;
        public Func<ValueTask> GlobalCleanupAction { get; set; } = null!;
        public Func<ValueTask> IterationSetupAction { get; set; } = null!;
        public Func<ValueTask> IterationCleanupAction { get; set; } = null!;

        /// <summary>
        /// The benchmark method(s), used by the jit stage to watch for their tier-up via JIT events.
        /// When empty (nothing to watch, or resolution failed), the jit stage falls back to a fixed delay.
        /// </summary>
        public IEnumerable<MethodInfo> WorkloadMethods { get; set; } = [];

        public Job TargetJob { get; set; } = Job.Default;
        public Diagnosers.CompositeInProcessDiagnoserHandler InProcessDiagnoserHandler { get; set; } = null!;
    }
}