using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
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
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, null, baseline)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null,
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, runStrategy, baseline)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RuntimeMoniker runtimeMoniker,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null,
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, null, baseline, runtimeMoniker)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            RuntimeMoniker runtimeMoniker,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null,
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, runStrategy, baseline, runtimeMoniker)) { }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int targetCount, int invocationCount, RunStrategy? runStrategy,
            bool baseline, RuntimeMoniker runtimeMoniker = RuntimeMoniker.HostProcess)
        {
            var job = new Job(id);
            if (launchCount != DefaultValue)
                job.Run.LaunchCount = launchCount;
            if (warmupCount != DefaultValue)
                job.Run.WarmupCount = warmupCount;
            if (targetCount != DefaultValue)
                job.Run.IterationCount = targetCount;
            if (invocationCount != DefaultValue)
            {
                job.Run.InvocationCount = invocationCount;
                int unrollFactor = job.Run.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);
                if (invocationCount % unrollFactor != 0)
                    job.Run.UnrollFactor = 1;
            }

            if (runStrategy != null)
                job.Run.RunStrategy = runStrategy.Value;
            if (baseline)
                job.Meta.Baseline = true;
            if (runtimeMoniker != RuntimeMoniker.HostProcess)
                job.Environment.Runtime = runtimeMoniker.GetRuntime();

            return job.Freeze();
        }
    }
}