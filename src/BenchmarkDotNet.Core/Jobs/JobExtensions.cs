using System;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Jobs
{
    public static class JobExtensions
    {
        // Env
        public static Job With(this Job job, Platform platform)
        {
            job.Env.Platform = platform;
            return job;
        }
        public static Job With(this Job job, Jit jit)
        {
            job.Env.Jit = jit;
            return job;
        }
        public static Job With(this Job job, Runtime runtime)
        {
            job.Env.Runtime = runtime;
            return job;
        }
        public static Job WithAffinity(this Job job, IntPtr affinity)
        {
            job.Env.Affinity = affinity;
            return job;
        }
        public static Job WithGcServer(this Job job, bool value)
        {
            job.Env.Gc.WithServer(value);
            return job;
        }
        public static Job WithGcConcurrent(this Job job, bool value)
        {
            job.Env.Gc.WithConcurrent(value);
            return job;
        }
        public static Job WithGcCpuGroups(this Job job, bool value)
        {
            job.Env.Gc.WithCpuGroups(value);
            return job;
        }
        public static Job WithGcForce(this Job job, bool value)
        {
            job.Env.Gc.WithForce(value);
            return job;
        }
        public static Job WithGcAllowVeryLargeObjects(this Job job, bool value)
        {
            job.Env.Gc.WithAllowVeryLargeObjects(value);
            return job;
        }
        public static Job With(this Job job, GcMode gc)
        {
            EnvMode.GcCharacteristic[job] = gc;
            return job;
        }

        // Run
        public static Job With(this Job job, RunStrategy strategy)
        {
            job.Run.RunStrategy = strategy;
            return job;
        }
        public static Job WithLaunchCount(this Job job, int count)
        {
            job.Run.LaunchCount = count;
            return job;
        }
        public static Job WithWarmupCount(this Job job, int count)
        {
            job.Run.WarmupCount = count;
            return job;
        }
        public static Job WithTargetCount(this Job job, int count)
        {
            job.Run.TargetCount = count;
            return job;
        }
        public static Job WithIterationTime(this Job job, TimeInterval time)
        {
            job.Run.IterationTime = time;
            return job;
        }
        public static Job WithInvocationCount(this Job job, int count)
        {
            job.Run.InvocationCount = count;
            return job;
        }
        public static Job WithUnrollFactor(this Job job, int factor)
        {
            job.Run.UnrollFactor = factor;
            return job;
        }

        // Infrastructure
        public static Job With(this Job job, IToolchain toolchain)
        {
            job.Infrastructure.Toolchain = toolchain;
            return job;
        }
        public static Job With(this Job job, IClock clock)
        {
            job.Infrastructure.Clock = clock;
            return job;
        }
        public static Job With(this Job job, IEngineFactory engineFactory)
        {
            job.Infrastructure.EngineFactory = engineFactory;
            return job;
        }

        // Accuracy
        public static Job WithMaxStdErrRelative(this Job job, double value)
        {
            job.Accuracy.MaxStdErrRelative = value;
            return job;
        }
        public static Job WithMinIterationTime(this Job job, TimeInterval value)
        {
            job.Accuracy.MinIterationTime = value;
            return job;
        }
        public static Job WithMinInvokeCount(this Job job, int value)
        {
            job.Accuracy.MinInvokeCount = value;
            return job;
        }
        public static Job WithEvaluateOverhead(this Job job, bool value)
        {
            job.Accuracy.EvaluateOverhead = value;
            return job;
        }
        public static Job WithRemoveOutliers(this Job job, bool value)
        {
            job.Accuracy.RemoveOutliers = value;
            return job;
        }
        public static Job WithAnalyzeLaunchVariance(this Job job, bool value)
        {
            job.Accuracy.AnalyzeLaunchVariance = value;
            return job;
        }

        // Info
        [Obsolete]
        public static string GetShortInfo(this Job job) => job.ResolvedId;
        [Obsolete]
        public static string GetFullInfo(this Job job) => CharacteristicSetPresenter.Default.ToPresentation(job);
    }
}