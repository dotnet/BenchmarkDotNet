using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class ScenarioJobAttribute : JobConfigBaseAttribute
    {
        [PublicAPI]
        public ScenarioJobAttribute(
            int launchCount = 15,
            int warmupCount = 1,
            string id = null,
            bool baseline = false
        ) : base(CreateJob(id, launchCount, warmupCount, baseline)) { }

        private static Job CreateJob(string id, int launchCount, int warmupCount, bool baseline)
        {
            var job = new Job(id);

            job.Run.LaunchCount = launchCount;
            job.Run.WarmupCount = warmupCount;
            job.Run.RunStrategy = RunStrategy.Monitoring;
            job.Meta.Baseline = baseline;
            job.Accuracy.EvaluateOverhead = false;
            job.Accuracy.OutlierMode = OutlierMode.None;

            return job.Freeze();
        }
    }
}