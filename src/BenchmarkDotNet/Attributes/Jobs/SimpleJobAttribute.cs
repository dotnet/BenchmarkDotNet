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
            int manualValuesCount = 0;

            if (launchCount != DefaultValue)
            {
                job.Run.LaunchCount = launchCount;
                manualValuesCount++;
            }

            if (warmupCount != DefaultValue)
            {
                job.Run.WarmupCount = warmupCount;
                manualValuesCount++;
            }

            if (targetCount != DefaultValue)
            {
                job.Run.IterationCount = targetCount;
                manualValuesCount++;
            }
            if (invocationCount != DefaultValue)
            {
                job.Run.InvocationCount = invocationCount;
                manualValuesCount++;

                int unrollFactor = job.Run.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);
                if (invocationCount % unrollFactor != 0)
                {
                    job.Run.UnrollFactor = 1;
                    manualValuesCount++;
                }
            }

            if (runStrategy != null)
            {
                job.Run.RunStrategy = runStrategy.Value;
                manualValuesCount++;
            }

            if (baseline)
                job.Meta.Baseline = true;

            if (runtimeMoniker != RuntimeMoniker.HostProcess)
            {
                job.Environment.Runtime = runtimeMoniker.GetRuntime();
                manualValuesCount++;
            }

            if (id == null && manualValuesCount == 1 && runtimeMoniker != RuntimeMoniker.HostProcess)
                job = job.WithId(runtimeMoniker.GetRuntime().Name);

            return job.Freeze();
        }
    }
}