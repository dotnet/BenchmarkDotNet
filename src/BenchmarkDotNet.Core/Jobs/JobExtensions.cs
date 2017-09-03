using System;
using System.Collections.Generic;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Jobs
{
    public static class JobExtensions
    {
        public static Job With(this Job job, Platform platform) => job.WithCore(j => j.Env.Platform = platform);
        public static Job WithId(this Job job, string id) => new Job(id, job);

        // Env
        public static Job With(this Job job, Jit jit) => job.WithCore(j => j.Env.Jit = jit);
        public static Job With(this Job job, Runtime runtime) => job.WithCore(j => j.Env.Runtime = runtime);
        public static Job WithAffinity(this Job job, IntPtr affinity) => job.WithCore(j => j.Env.Affinity = affinity);
        public static Job WithGcServer(this Job job, bool value) => job.WithCore(j => j.Env.Gc.Server = value);
        public static Job WithGcConcurrent(this Job job, bool value) => job.WithCore(j => j.Env.Gc.Concurrent = value);
        public static Job WithGcCpuGroups(this Job job, bool value) => job.WithCore(j => j.Env.Gc.CpuGroups = value);
        public static Job WithGcForce(this Job job, bool value) => job.WithCore(j => j.Env.Gc.Force = value);
        public static Job WithGcAllowVeryLargeObjects(this Job job, bool value) => job.WithCore(j => j.Env.Gc.AllowVeryLargeObjects = value);
        public static Job WithGcRetainVm(this Job job, bool value) => job.WithCore(j => j.Env.Gc.RetainVm = value);
        public static Job With(this Job job, GcMode gc) => job.WithCore(j => EnvMode.GcCharacteristic[j] = gc);

        // Run
        public static Job With(this Job job, RunStrategy strategy) => job.WithCore(j => j.Run.RunStrategy = strategy);
        public static Job WithLaunchCount(this Job job, int count) => job.WithCore(j => j.Run.LaunchCount = count);
        public static Job WithWarmupCount(this Job job, int count) => job.WithCore(j => j.Run.WarmupCount = count);
        public static Job WithTargetCount(this Job job, int count) => job.WithCore(j => j.Run.TargetCount = count);
        public static Job WithIterationTime(this Job job, TimeInterval time) => job.WithCore(j => j.Run.IterationTime = time);
        public static Job WithInvocationCount(this Job job, int count) => job.WithCore(j => j.Run.InvocationCount = count);
        public static Job WithUnrollFactor(this Job job, int factor) => job.WithCore(j => j.Run.UnrollFactor = factor);

        // Infrastructure
        public static Job With(this Job job, IToolchain toolchain) => job.WithCore(j => j.Infrastructure.Toolchain = toolchain);
        public static Job With(this Job job, IClock clock) => job.WithCore(j => j.Infrastructure.Clock = clock);
        public static Job With(this Job job, IEngineFactory engineFactory) => job.WithCore(j => j.Infrastructure.EngineFactory = engineFactory);
        public static Job WithCustomBuildConfiguration(this Job job, string buildConfiguraiton) => job.WithCore(j => j.Infrastructure.BuildConfiguration = buildConfiguraiton);
        public static Job With(this Job job, IReadOnlyList<EnvironmentVariable> environmentVariables) => job.WithCore(j => j.Infrastructure.EnvironmentVariables = environmentVariables);
        public static Job With(this Job job, IReadOnlyList<Argument> arguments) => job.WithCore(j => j.Infrastructure.Arguments = arguments);

        // Accuracy
        public static Job WithMaxRelativeError(this Job job, double value) => job.WithCore(j => j.Accuracy.MaxRelativeError= value);
        public static Job WithMaxAbsoluteError(this Job job, TimeInterval value) => job.WithCore(j => j.Accuracy.MaxAbsoluteError = value);
        public static Job WithMinIterationTime(this Job job, TimeInterval value) => job.WithCore(j => j.Accuracy.MinIterationTime = value);
        public static Job WithMinInvokeCount(this Job job, int value) => job.WithCore(j => j.Accuracy.MinInvokeCount = value);
        public static Job WithEvaluateOverhead(this Job job, bool value) => job.WithCore(j => j.Accuracy.EvaluateOverhead = value);
        public static Job WithRemoveOutliers(this Job job, bool value) => job.WithCore(j => j.Accuracy.RemoveOutliers = value);
        public static Job WithAnalyzeLaunchVariance(this Job job, bool value) => job.WithCore(j => j.Accuracy.AnalyzeLaunchVariance = value);
        

        // Info
        [Obsolete]
        public static string GetShortInfo(this Job job) => job.ResolvedId;
        [Obsolete]
        public static string GetFullInfo(this Job job) => CharacteristicSetPresenter.Default.ToPresentation(job);

        private static Job WithCore(this Job job, Action<Job> updateCallback)
        {
            job = new Job(job);
            updateCallback(job);
            return job;
        }
    }
}