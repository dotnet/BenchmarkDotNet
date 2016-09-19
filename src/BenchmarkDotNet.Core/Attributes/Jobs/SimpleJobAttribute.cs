using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class SimpleJobAttribute : JobConfigBaseAttribute
    {
        private const int DefaultValue = -1;

        public SimpleJobAttribute(
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, null))
        {
        }

        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = DefaultValue,
            int warmupCount = DefaultValue,
            int targetCount = DefaultValue,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, runStrategy))
        {
        }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int targetCount, RunStrategy? runStrategy)
        {
            var job = Job.Default;
            if (launchCount != DefaultValue)
                job = job.WithLaunchCount(launchCount);
            if (warmupCount != DefaultValue)
                job = job.WithWarmupCount(warmupCount);
            if (targetCount != DefaultValue)
                job = job.WithTargetCount(targetCount);
            if (runStrategy != null)
                job = job.With(runStrategy.Value);
            if (id != null)
                job = job.WithId(id);
            return job;
        }
    }
}