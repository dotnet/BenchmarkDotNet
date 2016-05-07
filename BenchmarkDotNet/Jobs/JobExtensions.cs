using BenchmarkDotNet.Toolchains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Jobs
{
    public static class JobExtensions
    {
        public static IJob With(this IJob job, Mode mode) => job.With(j => j.Mode = mode);
        public static IJob With(this IJob job, Platform platform) => job.With(j => j.Platform = platform);
        public static IJob With(this IJob job, Jit jit) => job.With(j => j.Jit = jit);
        public static IJob With(this IJob job, Framework framework) => job.With(j => j.Framework = framework);
        public static IJob With(this IJob job, IToolchain toolchain) => job.With(j => j.Toolchain = toolchain);
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

        public static Property[] GetAllProperties(this IJob job)
        {
            return new[]
            {
                new Property("Mode", job.Mode.ToString()),
                new Property("Platform", job.Platform.ToString()),
                new Property("Jit", job.Jit.ToString()),
                new Property("Framework", job.Framework.ToString()),
                new Property("Runtime", job.Runtime.ToString()),
                new Property("Warmup", job.WarmupCount.ToString()),
                new Property("Target", job.TargetCount.ToString()),
                new Property("Process", job.LaunchCount.ToString()),
                new Property("IterationTime", job.IterationTime.ToString()),
                new Property("Affinity", job.Affinity.ToString())
            };
        }

        public static string GetFullInfo(this IJob job) => string.Join("_", job.AllProperties.Select(p => $"{p.Name}-{p.Value}"));

        public static string GetShortInfo(this IJob job, IJob[] allJobs = null)
        {
            // TODO: make it automatically
            string shortInfo;
            if (TryGetShortInfoForPredefinedJobs(job, out shortInfo))
            {
                return shortInfo;
            }

            var defaultJobProperties = Job.Default.AllProperties;
            var ownProperties = job.AllProperties;

            var nonDefaultProperties = ownProperties
                .Where((ownProperty, propertyIndex) => ownProperty.Value != defaultJobProperties[propertyIndex].Value
                    && (allJobs == null || MoreThanOneJobHaveUniquePropertyValue(allJobs, propertyIndex)));

            return string.Join("_", nonDefaultProperties.Select(property => property.GetShortInfo()));
        }

        public static string GenerateWithDefinitions(this IJob job)
        {
            var builder = new StringBuilder(80);
            builder.Append($".With(BenchmarkDotNet.Jobs.Mode.{job.Mode})");
            builder.Append($".WithWarmupCount({job.WarmupCount.Value})");
            builder.Append($".WithTargetCount({job.TargetCount.Value})");
            builder.Append($".WithIterationTime({job.IterationTime.Value})");
            builder.Append($".WithLaunchCount({job.LaunchCount.Value})");
            return builder.ToString();
        }

        private static IJob With(this IJob job, Action<Job> set)
        {
            var newJob = job.Clone();
            set(newJob);
            return newJob;
        }

        private static Job Clone(this IJob job) => new Job
        {
            Jit = job.Jit,
            Platform = job.Platform,
            Toolchain = job.Toolchain,
            Framework = job.Framework,
            Runtime = job.Runtime,
            Mode = job.Mode,
            LaunchCount = job.LaunchCount,
            TargetCount = job.TargetCount,
            WarmupCount = job.WarmupCount,
            IterationTime = job.IterationTime,
            Affinity = job.Affinity
        };

        private static bool TryGetShortInfoForPredefinedJobs(IJob job, out string shortInfo)
        {
            shortInfo = null;

            if (job.Equals(Job.LegacyJitX86))
                shortInfo = "LegacyX86";
            if (job.Equals(Job.LegacyJitX64))
                shortInfo = "LegacyX64";
            if (job.Equals(Job.RyuJitX64))
                shortInfo = "RyuJitX64";
            if (job.Equals(Job.Dry))
                shortInfo = "Dry";
            if (job.Equals(Job.Mono))
                shortInfo = "Mono";
            if (job.Equals(Job.Clr))
                shortInfo = "Clr";
            if (job.Equals(Job.Dnx))
                shortInfo = "Dnx";
            if (job.Equals(Job.Core))
                shortInfo = "Core";

            return !string.IsNullOrEmpty(shortInfo);
        }

        private static bool MoreThanOneJobHaveUniquePropertyValue(IJob[] allJobs, int propertyIndex)
        {
            if (allJobs.Length <= 1)
            {
                return false;
            }

            var uniqueValues = new HashSet<string>();
            for (int i = 0; i < allJobs.Length; i++)
            {
                var value = allJobs[i].AllProperties[propertyIndex].Value;
                if (uniqueValues.Contains(value))
                {
                    continue;
                }

                if (uniqueValues.Count > 0)
                {
                    return true;
                }
                uniqueValues.Add(value);
            }

            return false;
        }
    }
}