using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Engines
{
    public class EngineResolver : Resolver
    {
        internal const int DefaultMinWorkloadIterationCount = 15;
        internal const int DefaultMaxWorkloadIterationCount = 100;

        internal const int ForceAutoWarmup = -1;
        internal const int DefaultMinWarmupIterationCount = 6;
        internal const int DefaultMaxWarmupIterationCount = 50;

        public static readonly IResolver Instance = new EngineResolver();

        private EngineResolver()
        {
            Register(RunMode.RunStrategyCharacteristic, () => RunStrategy.Throughput);
            Register(RunMode.IterationTimeCharacteristic, () => TimeInterval.Millisecond * 500);

            Register(RunMode.MinIterationCountCharacteristic, () => DefaultMinWorkloadIterationCount);
            Register(RunMode.MaxIterationCountCharacteristic, () => DefaultMaxWorkloadIterationCount);

            Register(RunMode.MinWarmupIterationCountCharacteristic, () => DefaultMinWarmupIterationCount);
            Register(RunMode.MaxWarmupIterationCountCharacteristic, () => DefaultMaxWarmupIterationCount);

            Register(AccuracyMode.MaxRelativeErrorCharacteristic, () => 0.02);
            Register(AccuracyMode.MinIterationTimeCharacteristic, () => TimeInterval.Millisecond * 500);
            Register(AccuracyMode.MinInvokeCountCharacteristic, () => 4);
            Register(AccuracyMode.EvaluateOverheadCharacteristic, () => true);
            Register(AccuracyMode.OutlierModeCharacteristic, job =>
            {
                var strategy = job.ResolveValue(RunMode.RunStrategyCharacteristic, this);
                switch (strategy)
                {
                    case RunStrategy.Throughput:
                        return OutlierMode.RemoveUpper;
                    case RunStrategy.ColdStart:
                    case RunStrategy.Monitoring:
                        return OutlierMode.DontRemove;
                    default:
                        throw new NotSupportedException($"Unknown runStrategy: {strategy}");
                }
            });
        }
    }
}