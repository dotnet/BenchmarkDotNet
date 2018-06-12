﻿using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Engines
{
    public class EngineResolver : Resolver
    {
        internal const int DefaultMinTargetIterationCount = 15;
        internal const int DefaultMaxTargetIterationCount = 100;
        
        public static readonly IResolver Instance = new EngineResolver();

        private EngineResolver()
        {
            Register(RunMode.RunStrategyCharacteristic, () => RunStrategy.Throughput);
            Register(RunMode.IterationTimeCharacteristic, () => TimeInterval.Millisecond * 500);

            Register(RunMode.MinTargetIterationCountCharacteristic, () => DefaultMinTargetIterationCount);
            Register(RunMode.MaxTargetIterationCountCharacteristic, () => DefaultMaxTargetIterationCount);

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
                        return OutlierMode.OnlyUpper;
                    case RunStrategy.ColdStart:
                    case RunStrategy.Monitoring:
                        return OutlierMode.None;
                    default:
                        throw new NotSupportedException($"Unknown runStrategy: {strategy}");
                }
            });
        }
    }
}