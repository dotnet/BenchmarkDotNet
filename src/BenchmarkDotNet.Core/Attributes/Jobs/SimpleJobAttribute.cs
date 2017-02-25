using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class SimpleJobAttribute : JobConfigBaseAttribute
    {
        private const int DefaultValue = -1;

        [PublicAPI]
        public SimpleJobAttribute(
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, null))
        {
        }

        [PublicAPI]
        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            int invocationCount = DefaultValue,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, invocationCount, runStrategy))
        {
        }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int targetCount, int invocationCount, RunStrategy? runStrategy)
        {
            var job = new Job(id);
            if (launchCount != DefaultValue)
                job.Run.LaunchCount = launchCount;
            if (warmupCount != DefaultValue)
                job.Run.WarmupCount = warmupCount;
            if (targetCount != DefaultValue)
                job.Run.TargetCount = targetCount;
            if (invocationCount != DefaultValue)
                job.Run.InvocationCount = invocationCount;
            if (runStrategy != null)
                job.Run.RunStrategy = runStrategy.Value;

            return job.Freeze();
        }
    }
}