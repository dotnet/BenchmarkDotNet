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
            var job = new Job(id);
            if (launchCount != DefaultValue)
                job.Run.LaunchCount = launchCount;
            if (warmupCount != DefaultValue)
                job.Run.WarmupCount = warmupCount;
            if (targetCount != DefaultValue)
                job.Run.TargetCount = targetCount;
            if (runStrategy != null)
                job.Run.RunStrategy = runStrategy.Value;

            return job.Freeze();
        }
    }
}