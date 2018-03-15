﻿using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class SimpleJobAttribute : JobConfigBaseAttribute
    {
        private const int DefaultValue = -1;

        [PublicAPI]
        public SimpleJobAttribute(
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null,
            bool isBaseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, null, isBaseline)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null,
            bool isBaseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, runStrategy, isBaseline)) { }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int targetCount, int invocationCount, RunStrategy? runStrategy,
            bool isBaseline)
        {
            var job = new Job(id);
            if (launchCount != DefaultValue)
                job.Run.LaunchCount = launchCount;
            if (warmupCount != DefaultValue)
                job.Run.WarmupCount = warmupCount;
            if (targetCount != DefaultValue)
                job.Run.TargetCount = targetCount;
            if (invocationCount != DefaultValue)
            {
                job.Run.InvocationCount = invocationCount;
                int unrollFactor = job.Run.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvResolver.Instance);
                if (invocationCount % unrollFactor != 0)
                    job.Run.UnrollFactor = 1;
            }

            if (runStrategy != null)
                job.Run.RunStrategy = runStrategy.Value;
            if (isBaseline)
                job.Meta.IsBaseline = true;

            return job.Freeze();
        }
    }
}