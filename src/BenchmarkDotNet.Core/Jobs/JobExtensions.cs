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
        // General
        public static Job With<T>(this Job job, ICharacteristic<T> characteristic) => Job.Parse(job.ToSet().Mutate(characteristic));

        // Env
        public static Job With(this Job job, Platform platform) => job.With(job.Env.Platform.Mutate(platform));
        public static Job With(this Job job, Jit jit) => job.With(job.Env.Jit.Mutate(jit));
        public static Job With(this Job job, Runtime runtime) => job.With(job.Env.Runtime.Mutate(runtime));
        public static Job WithAffinity(this Job job, IntPtr affinity) => job.With(job.Env.Affinity.Mutate(affinity));
        public static Job WithGcServer(this Job job, bool value) => job.With(job.Env.Gc.Server.Mutate(value));
        public static Job WithGcConcurrent(this Job job, bool value) => job.With(job.Env.Gc.Concurrent.Mutate(value));
        public static Job WithGcCpuGroups(this Job job, bool value) => job.With(job.Env.Gc.CpuGroups.Mutate(value));
        public static Job WithGcForce(this Job job, bool value) => job.With(job.Env.Gc.Force.Mutate(value));
        public static Job WithGcAllowVeryLargeObjects(this Job job, bool value) => job.With(job.Env.Gc.AllowVeryLargeObjects.Mutate(value));
        public static Job With(this Job job, GcMode gc) => job.Mutate(gc.ToMutator());

        // Run
        public static Job With(this Job job, RunStrategy strategy) => job.With(job.Run.RunStrategy.Mutate(strategy));
        public static Job WithLaunchCount(this Job job, int count) => job.With(job.Run.LaunchCount.Mutate(count));
        public static Job WithWarmupCount(this Job job, int count) => job.With(job.Run.WarmupCount.Mutate(count));
        public static Job WithTargetCount(this Job job, int count) => job.With(job.Run.TargetCount.Mutate(count));
        public static Job WithIterationTime(this Job job, TimeInterval time) => job.With(job.Run.IterationTime.Mutate(time));
        public static Job WithInvocationCount(this Job job, int count) => job.With(job.Run.InvocationCount.Mutate(count));

        // Infra
        public static Job With(this Job job, IToolchain toolchain) => job.With(job.Infra.Toolchain.Mutate(toolchain));
        public static Job With(this Job job, IClock clock) => job.With(job.Infra.Clock.Mutate(clock));
        public static Job With(this Job job, IEngine engine) => job.With(job.Infra.Engine.Mutate(engine));

        // Accuracy
        public static Job WithMaxStdErrRelative(this Job job, double value) => job.With(job.Accuracy.MaxStdErrRelative.Mutate(value));
        public static Job WithMinIterationTime(this Job job, TimeInterval value) => job.With(job.Accuracy.MinIterationTime.Mutate(value));
        public static Job WithMinInvokeCount(this Job job, int value) => job.With(job.Accuracy.MinInvokeCount.Mutate(value));
        public static Job WithEvaluateOverhead(this Job job, bool value) => job.With(job.Accuracy.EvaluateOverhead.Mutate(value));
        public static Job WithRemoveOutliers(this Job job, bool value) => job.With(job.Accuracy.RemoveOutliers.Mutate(value));

        // Info
        [Obsolete]
        public static string GetShortInfo(this Job job) => job.ResolvedId;
        [Obsolete]
        public static string GetFullInfo(this Job job) => CharacteristicSetPresenter.Default.ToPresentation(job.ToSet());
    }
}