using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Jobs
{
    public static class JobExtensions
    {
        public static IJob With(this IJob job, IToolchain toolchain) => job.With(j => j.Toolchain = toolchain);
        public static IJob With(this IJob job, Mode mode) => job.With(j => j.Mode = mode);
        public static IJob With(this IJob job, Platform platform) => job.With(j => j.Platform = platform);
        public static IJob With(this IJob job, Jit jit) => job.With(j => j.Jit = jit);
        public static IJob With(this IJob job, Framework framework) => job.With(j => j.Framework = framework);
        public static IJob With(this IJob job, Runtime runtime) => job.With(j => j.Runtime = runtime);
        public static IJob WithLaunchCount(this IJob job, Count launchCount) => job.With(j => j.LaunchCount = launchCount);
        public static IJob WithWarmupCount(this IJob job, Count warmupCount) => job.With(j => j.WarmupCount = warmupCount);
        public static IJob WithTargetCount(this IJob job, Count targetCount) => job.With(j => j.TargetCount = targetCount);
        public static IJob WithAffinity(this IJob job, Count affinity) => job.With(j => j.Affinity = affinity);

        /// <summary>
        /// Create a new job as a copy of the original job with specific time of a single iteration
        /// </summary>
        /// <param name="job">Original job</param>
        /// <param name="iterationTime">Iteration time in Millisecond or Auto</param>
        /// <returns></returns>
        public static IJob WithIterationTime(this IJob job, Count iterationTime) => job.With(j => j.IterationTime = iterationTime);

        private static Job Clone(this IJob job) => new Job
        {
            Jit = job.Jit,
            Platform = job.Platform,
            Framework = job.Framework,
            Toolchain = job.Toolchain,
            Runtime = job.Runtime,
            Mode = job.Mode,
            LaunchCount = job.LaunchCount,
            TargetCount = job.TargetCount,
            WarmupCount = job.WarmupCount,
            IterationTime = job.IterationTime,
            Affinity = job.Affinity
        };

        private static IJob With(this IJob job, Action<Job> set)
        {
            var newJob = job.Clone();
            set(newJob);
            return newJob;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetAllProperties(this IJob job)
        {
            yield return new KeyValuePair<string, string>("Toolchain", job.Toolchain?.Name ?? "Default");
            yield return new KeyValuePair<string, string>("Mode", job.Mode.ToString());
            yield return new KeyValuePair<string, string>("Platform", job.Platform.ToString());
            yield return new KeyValuePair<string, string>("Jit", job.Jit.ToString());
            yield return new KeyValuePair<string, string>("Framework", job.Framework.ToString());
            yield return new KeyValuePair<string, string>("Runtime", job.Runtime.ToString());
            yield return new KeyValuePair<string, string>("Warmup", job.WarmupCount.ToString());
            yield return new KeyValuePair<string, string>("Target", job.TargetCount.ToString());
            yield return new KeyValuePair<string, string>("Process", job.LaunchCount.ToString());
            yield return new KeyValuePair<string, string>("IterationTime", job.IterationTime.ToString());
            yield return new KeyValuePair<string, string>("Affinity", job.Affinity.ToString());
        }

        public static string GetFullInfo(this IJob job) => string.Join("_", job.GetAllProperties().Select(p => $"{p.Key}-{p.Value}"));

        public static string GetShortInfo(this IJob job)
        {
            // TODO: make it automatically
            if (job.Equals(Job.LegacyJitX86))
                return "LegacyX86";
            if (job.Equals(Job.LegacyJitX64))
                return "LegacyX64";
            if (job.Equals(Job.RyuJitX64))
                return "RyuJitX64";
            if (job.Equals(Job.Dry))
                return "Dry";
            if (job.Equals(Job.Mono))
                return "Mono";
            if (job.Equals(Job.Clr))
                return "Clr";
            var defaultJobProperties = Job.Default.GetAllProperties().ToArray();
            var ownProperties = job.GetAllProperties().ToArray();
            var n = ownProperties.Length;
            var targetProperties = Enumerable.Range(0, n).
                Where(i => ownProperties[i].Value != defaultJobProperties[i].Value).
                Select(i => ownProperties[i]);
            return string.Join("_", targetProperties.Select(GetShortInfoForProperty));
        }

        public static string GetShortInfo(this IJob job, IList<IJob> allJobs)
        {
            // TODO: make it automatically
            if (job.Equals(Job.LegacyJitX86))
                return "LegacyX86";
            if (job.Equals(Job.LegacyJitX64))
                return "LegacyX64";
            if (job.Equals(Job.RyuJitX64))
                return "RyuJitX64";
            if (job.Equals(Job.Dry))
                return "Dry";
            if (job.Equals(Job.Mono))
                return "Mono";
            if (job.Equals(Job.Clr))
                return "Clr";
            var defaultJobProperties = Job.Default.GetAllProperties().ToArray();
            var ownProperties = job.GetAllProperties().ToArray();
            var n = ownProperties.Length;
            var targetProperties = Enumerable.Range(0, n).
                Where(i => ownProperties[i].Value != defaultJobProperties[i].Value &&
                           allJobs.Select(j => j.GetAllProperties().ToArray()[i].Value).Distinct().Count() > 1).
                Select(i => ownProperties[i]);
            return string.Join("_", targetProperties.Select(GetShortInfoForProperty));
        }


        private static string GetShortInfoForProperty(KeyValuePair<string, string> property)
        {
            switch (property.Key)
            {
                case "Toolchain":
                    return property.Value + "Toolchain";
                case "Mode":
                case "Platform":
                    return property.Value;
                case "Warmup":
                case "Target":
                case "Process":
                case "IterationTime":
                case "Affinity":
                    return property.Key + property.Value;
            }
            return $"{property.Key}-{property.Value}";
        }

        // TODO: make toolchain
        public static string GenerateWithDefinitions(this IJob job)
        {
            var builder = new StringBuilder();
            builder.Append($".With(BenchmarkDotNet.Jobs.Mode.{job.Mode})");
            builder.Append($".WithWarmupCount({job.WarmupCount.Value})");
            builder.Append($".WithTargetCount({job.TargetCount.Value})");
            builder.Append($".WithIterationTime({job.IterationTime.Value})");
            builder.Append($".WithLaunchCount({job.LaunchCount.Value})");
            return builder.ToString();
        }
    }
}