using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    public class EngineResolver: Resolver
    {
        public static readonly IResolver Instance = new EngineResolver();

        private EngineResolver()
        {
            var run = Job.Default.Run;
            Register(run.RunStrategy, () => RunStrategy.Throughput);
            Register(run.IterationTime, () => TimeInterval.Millisecond * 200);

            var acc = Job.Default.Accuracy;
            Register(acc.MaxStdErrRelative, () => 0.01);
            Register(acc.MinIterationTime, () => TimeInterval.Millisecond * 200);
            Register(acc.MinInvokeCount, () => 4);
            Register(acc.EvaluateOverhead, () => true);
            Register(acc.RemoveOutliers, () => true);
        }
    }
}
