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
            int iterationCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = "",
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, iterationCount, invocationCount, null, baseline)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int iterationCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = "",
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, iterationCount, invocationCount, runStrategy, baseline)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            string runtimeMoniker,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int iterationCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = "",
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, iterationCount, invocationCount, null, baseline, runtimeMoniker)) { }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            string runtimeMoniker,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int iterationCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = "",
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, iterationCount, invocationCount, runStrategy, baseline, runtimeMoniker)) { }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int iterationCount, int invocationCount, RunStrategy? runStrategy,
            bool baseline, string? runtimeMoniker = null)
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

            if (iterationCount != DefaultValue)
            {
                job.Run.IterationCount = iterationCount;
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

            if (runtimeMoniker.IsNotBlank())
            {
                job.Infrastructure.Runtime = Runtime.Parse(runtimeMoniker);
                manualValuesCount++;
            }

            if (id == null && manualValuesCount == 1 && runtimeMoniker.IsNotBlank())
                job = job.WithId(Runtime.Parse(runtimeMoniker).ToString());

            return job.Freeze();
        }
    }
}