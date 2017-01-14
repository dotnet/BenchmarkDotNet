using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    public class EngineResolver : Resolver
    {
        public static readonly IResolver Instance = new EngineResolver();

        private EngineResolver()
        {
            Register(RunMode.RunStrategyCharacteristic, () => RunStrategy.Throughput);
            Register(RunMode.IterationTimeCharacteristic, () => TimeInterval.Millisecond * 200);

            Register(AccuracyMode.MaxStdErrRelativeCharacteristic, () => 0.01);
            Register(AccuracyMode.MinIterationTimeCharacteristic, () => TimeInterval.Millisecond * 200);
            Register(AccuracyMode.MinInvokeCountCharacteristic, () => 4);
            Register(AccuracyMode.EvaluateOverheadCharacteristic, () => true);
            Register(AccuracyMode.RemoveOutliersCharacteristic, job =>
            {
                var strategy = job.ResolveValue(RunMode.RunStrategyCharacteristic, this);
                switch (strategy)
                {
                    case RunStrategy.Throughput:
                        return true;
                    case RunStrategy.ColdStart:
                        return false;
                    default:
                        throw new NotSupportedException($"Unknown runStrategy: {strategy}");
                }
            });
        }
    }
}