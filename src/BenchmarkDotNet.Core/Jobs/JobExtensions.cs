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
        public static Job WithId(this Job job, string id) => new Job(id, job);

        // Env
        public static Job With(this Job job, Platform platform)
        {
            job = new Job(job);
            job.Env.Platform = platform;
            return job;
        }
        public static Job With(this Job job, Jit jit)
        {
            job = new Job(job);
            job.Env.Jit = jit;
            return job;
        }
        public static Job With(this Job job, Runtime runtime)
        {
            job = new Job(job);
            job.Env.Runtime = runtime;
            return job;
        }
        public static Job WithAffinity(this Job job, IntPtr affinity)
        {
            job = new Job(job);
            job.Env.Affinity = affinity;
            return job;
        }
        public static Job WithGcServer(this Job job, bool value)
        {
            job = new Job(job);
            job.Env.Gc.Server = value;
            return job;
        }
        public static Job WithGcConcurrent(this Job job, bool value)
        {
            job = new Job(job);
            job.Env.Gc.Concurrent = value;
            return job;
        }
        public static Job WithGcCpuGroups(this Job job, bool value)
        {
            job = new Job(job);
            job.Env.Gc.CpuGroups = value;
            return job;
        }
        public static Job WithGcForce(this Job job, bool value)
        {
            job = new Job(job);
            job.Env.Gc.Force = value;
            return job;
        }
        public static Job WithGcAllowVeryLargeObjects(this Job job, bool value)
        {
            job = new Job(job);
            job.Env.Gc.AllowVeryLargeObjects = value;
            return job;
        }
        public static Job With(this Job job, GcMode gc)
        {
            job = new Job(job);
            EnvMode.GcCharacteristic[job] = gc;
            return job;
        }

        // Run
        public static Job With(this Job job, RunStrategy strategy)
        {
            job = new Job(job);
            job.Run.RunStrategy = strategy;
            return job;
        }
        public static Job WithLaunchCount(this Job job, int count)
        {
            job = new Job(job);
            job.Run.LaunchCount = count;
            return job;
        }
        public static Job WithWarmupCount(this Job job, int count)
        {
            job = new Job(job);
            job.Run.WarmupCount = count;
            return job;
        }
        public static Job WithTargetCount(this Job job, int count)
        {
            job = new Job(job);
            job.Run.TargetCount = count;
            return job;
        }
        public static Job WithIterationTime(this Job job, TimeInterval time)
        {
            job = new Job(job);
            job.Run.IterationTime = time;
            return job;
        }
        public static Job WithInvocationCount(this Job job, int count)
        {
            job = new Job(job);
            job.Run.InvocationCount = count;
            return job;
        }
        public static Job WithUnrollFactor(this Job job, int factor)
        {
            job = new Job(job);
            job.Run.UnrollFactor = factor;
            return job;
        }

        // Infrastructure
        public static Job With(this Job job, IToolchain toolchain)
        {
            job = new Job(job);
            job.Infrastructure.Toolchain = toolchain;
            return job;
        }
        public static Job With(this Job job, IClock clock)
        {
            job = new Job(job);
            job.Infrastructure.Clock = clock;
            return job;
        }
        public static Job With(this Job job, IEngineFactory engineFactory)
        {
            job = new Job(job);
            job.Infrastructure.EngineFactory = engineFactory;
            return job;
        }

        // Accuracy
        public static Job WithMaxStdErrRelative(this Job job, double value)
        {
            job = new Job(job);
            job.Accuracy.MaxStdErrRelative = value;
            return job;
        }
        public static Job WithMinIterationTime(this Job job, TimeInterval value)
        {
            job = new Job(job);
            job.Accuracy.MinIterationTime = value;
            return job;
        }
        public static Job WithMinInvokeCount(this Job job, int value)
        {
            job = new Job(job);
            job.Accuracy.MinInvokeCount = value;
            return job;
        }
        public static Job WithEvaluateOverhead(this Job job, bool value)
        {
            job = new Job(job);
            job.Accuracy.EvaluateOverhead = value;
            return job;
        }
        public static Job WithRemoveOutliers(this Job job, bool value)
        {
            job = new Job(job);
            job.Accuracy.RemoveOutliers = value;
            return job;
        }
        public static Job WithAnalyzeLaunchVariance(this Job job, bool value)
        {
            job = new Job(job);
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