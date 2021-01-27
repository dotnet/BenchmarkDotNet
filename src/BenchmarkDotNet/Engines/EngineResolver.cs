using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    public class EngineResolver : Resolver
    {
        internal const int DefaultMinWorkloadIterationCount = 15;
        internal const int DefaultMaxWorkloadIterationCount = 100;
        internal const int DefaultIterationTime = 500;

        internal const int ForceAutoWarmup = -1;
        internal const int DefaultMinWarmupIterationCount = 6;
        internal const int DefaultMaxWarmupIterationCount = 50;

        public static readonly IResolver Instance = new EngineResolver();

        private EngineResolver()
        {
            Register(RunMode.RunStrategyCharacteristic, () => RunStrategy.Throughput);
            Register(RunMode.IterationTimeCharacteristic, () => TimeInterval.Millisecond * DefaultIterationTime);

            Register(RunMode.MinIterationCountCharacteristic, () => DefaultMinWorkloadIterationCount);
            Register(RunMode.MaxIterationCountCharacteristic, () => DefaultMaxWorkloadIterationCount);

            Register(RunMode.MinWarmupIterationCountCharacteristic, () => DefaultMinWarmupIterationCount);
            Register(RunMode.MaxWarmupIterationCountCharacteristic, () => DefaultMaxWarmupIterationCount);

            Register(AccuracyMode.MaxRelativeErrorCharacteristic, () => 0.02);
            Register(AccuracyMode.MinIterationTimeCharacteristic, () => TimeInterval.Millisecond * 500);
            Register(AccuracyMode.MinInvokeCountCharacteristic, () => 4);
            Register(AccuracyMode.EvaluateOverheadCharacteristic, () => true);
            Register(RunMode.MemoryRandomizationCharacteristic, () => false);
            Register(AccuracyMode.OutlierModeCharacteristic, job =>
            {
                // if Memory Randomization was enabled and the benchmark is truly multimodal
                // removing outliers could remove some values that are not actually outliers
                // see https://github.com/dotnet/BenchmarkDotNet/pull/1587#issue-516837573 for example
                if (job.ResolveValue(RunMode.MemoryRandomizationCharacteristic, this))
                    return OutlierMode.DontRemove;

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