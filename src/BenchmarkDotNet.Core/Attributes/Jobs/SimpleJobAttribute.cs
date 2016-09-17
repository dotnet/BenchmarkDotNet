using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class SimpleJobAttribute : JobConfigBaseAttribute
    {
        public SimpleJobAttribute(
            int launchCount = -1,
            int warmupCount = -1,
            int targetCount = -1,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, null))
        {
        }

        public SimpleJobAttribute(
            RunStrategy runStrategy,
            int launchCount = -1,
            int warmupCount = -1,
            int targetCount = -1,
            string id = null
        ) : base(CreateJob(id, launchCount, warmupCount, targetCount, runStrategy))
        {
        }

        private static Job CreateJob(string id, int launchCount, int warmupCount, int targetCount, RunStrategy? runStrategy)
        {
            var job = Job.Default;
            if (launchCount >= 0)
                job = job.WithLaunchCount(launchCount);
            if (warmupCount >= 0)
                job = job.WithWarmupCount(warmupCount);
            if (targetCount >= 0)
                job = job.WithTargetCount(targetCount);
            if (runStrategy != null)
                job = job.With(runStrategy.Value);
            if (id != null)
                job = job.WithId(id);
            return job;
        }
    }
}