using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public static class JobExtensions
    {
        public static Job With(this Job job, Platform platform) => job.WithCore(j => j.Environment.Platform = platform);
        public static Job WithId(this Job job, string id) => new Job(id, job);

        // Env
        public static Job With(this Job job, Jit jit) => job.WithCore(j => j.Environment.Jit = jit);
        public static Job With(this Job job, Runtime runtime) => job.WithCore(j => j.Environment.Runtime = runtime);

        /// <summary>
        /// ProcessorAffinity for the benchmark process.
        /// See also: https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx
        /// </summary>
        public static Job WithAffinity(this Job job, IntPtr affinity) => job.WithCore(j => j.Environment.Affinity = affinity);

        /// <summary>
        /// Specifies whether the common language runtime runs server garbage collection.
        /// <value>false: Does not run server garbage collection. This is the default.</value>
        /// <value>true: Runs server garbage collection.</value>
        /// </summary>
        public static Job WithGcServer(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.Server = value);

        /// <summary>
        /// Specifies whether the common language runtime runs garbage collection on a separate thread.
        /// <value>false: Does not run garbage collection concurrently.</value>
        /// <value>true: Runs garbage collection concurrently. This is the default.</value>
        /// </summary>
        public static Job WithGcConcurrent(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.Concurrent = value);

        /// <summary>
        /// Specifies whether garbage collection supports multiple CPU groups.
        /// <value>false: Garbage collection does not support multiple CPU groups. This is the default.</value>
        /// <value>true: Garbage collection supports multiple CPU groups, if server garbage collection is enabled.</value>
        /// </summary>
        [PublicAPI]
        public static Job WithGcCpuGroups(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.CpuGroups = value);

        /// <summary>
        /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
        /// <value>false: Does not force garbage collection.</value>
        /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
        /// </summary>
        public static Job WithGcForce(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.Force = value);

        /// <summary>
        /// On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size.
        /// <value>false: Arrays greater than 2 GB in total size are not enabled. This is the default.</value>
        /// <value>true: Arrays greater than 2 GB in total size are enabled on 64-bit platforms.</value>
        /// </summary>
        public static Job WithGcAllowVeryLargeObjects(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.AllowVeryLargeObjects = value);

        /// <summary>
        /// Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS
        /// <remarks>The default is false</remarks>
        /// </summary>
        [PublicAPI]
        public static Job WithGcRetainVm(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.RetainVm = value);

        /// <summary>
        ///  specify the # of Server GC threads/heaps, must be smaller than the # of logical CPUs the process is allowed to run on, 
        ///  ie, if you don't specifically affinitize your process it means the # of total logical CPUs on the machine; 
        ///  otherwise this is the # of logical CPUs you affinitized your process to.
        /// </summary>
        [PublicAPI]
        public static Job WithHeapCount(this Job job, int heapCount) => job.WithCore(j => j.Environment.Gc.HeapCount = heapCount);

        /// <summary>
        /// specify true to disable hard affinity of Server GC threads to CPUs
        /// </summary>
        [PublicAPI]
        public static Job WithNoAffinitize(this Job job, bool value) => job.WithCore(j => j.Environment.Gc.NoAffinitize = value);

        /// <summary>
        /// process mask, see <see href="https://support.microsoft.com/en-us/help/4014604/may-2017-description-of-the-quality-rollup-for-the-net-framework-4-6-4">MSDN</see> for more.
        /// </summary>
        [PublicAPI]
        public static Job WithHeapAffinitizeMask(this Job job, int heapAffinitizeMask) =>
            job.WithCore(j => j.Environment.Gc.HeapAffinitizeMask = heapAffinitizeMask);

        [PublicAPI]
        public static Job With(this Job job, GcMode gc) => job.WithCore(j => EnvironmentMode.GcCharacteristic[j] = gc);

        // Run
        /// <summary>
        /// Available values: Throughput and ColdStart.
        ///     Throughput: default strategy which allows to get good precision level.
        ///     ColdStart: should be used only for measuring cold start of the application or testing purpose.
        ///     Monitoring: no overhead evaluating, with several target iterations. Perfect for macrobenchmarks without a steady state with high variance.
        /// </summary>
        public static Job With(this Job job, RunStrategy strategy) => job.WithCore(j => j.Run.RunStrategy = strategy);

        /// <summary>
        /// How many times we should launch process with target benchmark.
        /// </summary>
        public static Job WithLaunchCount(this Job job, int count) => job.WithCore(j => j.Run.LaunchCount = count);

        /// <summary>
        /// How many warmup iterations should be performed.
        /// </summary>
        public static Job WithWarmupCount(this Job job, int count) => job.WithCore(j => j.Run.WarmupCount = count);

        /// <summary>
        /// Minimum count of warmup iterations that should be performed
        /// The default value is 6
        /// </summary>
        public static Job WithMinWarmupCount(this Job job, int count) => job.WithCore(j => j.Run.MinWarmupIterationCount = count);

        /// <summary>
        /// Maximum count of warmup iterations that should be performed
        /// The default value is 50
        /// </summary>
        public static Job WithMaxWarmupCount(this Job job, int count) => job.WithCore(j => j.Run.MaxWarmupIterationCount = count);

        [Obsolete("Please use WithIterationCount instead (rename)"), PublicAPI]
        public static Job WithTargetCount(this Job job, int count) => job.WithCore(j => j.Run.IterationCount = count);

        /// <summary>
        /// How many target iterations should be performed.
        /// If specified, <see cref="RunMode.MinIterationCount"/> will be ignored.
        /// If specified, <see cref="RunMode.MaxIterationCount"/> will be ignored.
        /// </summary>
        public static Job WithIterationCount(this Job job, int count) => job.WithCore(j => j.Run.IterationCount = count);

        /// <summary>
        /// Desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration.
        /// The default value is 500 milliseconds.
        /// </summary>
        public static Job WithIterationTime(this Job job, TimeInterval time) => job.WithCore(j => j.Run.IterationTime = time);

        /// <summary>
        /// Invocation count in a single iteration.
        /// If specified, <see cref="RunMode.IterationTime"/> will be ignored.
        /// If specified, it must be a multiple of <see cref="RunMode.UnrollFactor"/>.
        /// </summary>
        public static Job WithInvocationCount(this Job job, int count) => job.WithCore(j => j.Run.InvocationCount = count);

        /// <summary>
        /// How many times the benchmark method will be invoked per one iteration of a generated loop.
        /// The default value is 16.
        /// </summary>
        public static Job WithUnrollFactor(this Job job, int factor) => job.WithCore(j => j.Run.UnrollFactor = factor);

        /// <summary>
        /// Run the benchmark exactly once per iteration.
        /// </summary>
        public static Job RunOncePerIteration(this Job job) => job.WithInvocationCount(1).WithUnrollFactor(1);

        /// <summary>
        /// Minimum count of target iterations that should be performed.
        /// The default value is 15.
        /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/> is not going to work.</remarks>
        /// </summary>
        public static Job WithMinIterationCount(this Job job, int count) => job.WithCore(j => j.Run.MinIterationCount = count);

        /// <summary>
        /// Maximum count of target iterations that should be performed.
        /// The default value is 100.
        /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/>  is not going to work.</remarks>
        /// </summary>
        public static Job WithMaxIterationCount(this Job job, int count) => job.WithCore(j => j.Run.MaxIterationCount = count);

        // Infrastructure
        public static Job With(this Job job, IToolchain toolchain) => job.WithCore(j => j.Infrastructure.Toolchain = toolchain);
        [PublicAPI] public static Job With(this Job job, IClock clock) => job.WithCore(j => j.Infrastructure.Clock = clock);
        [PublicAPI] public static Job With(this Job job, IEngineFactory engineFactory) => job.WithCore(j => j.Infrastructure.EngineFactory = engineFactory);

        public static Job WithCustomBuildConfiguration(this Job job, string buildConfiguration) =>
            job.WithCore(j => j.Infrastructure.BuildConfiguration = buildConfiguration);

        public static Job With(this Job job, IReadOnlyList<EnvironmentVariable> environmentVariables) =>
            job.WithCore(j => j.Environment.EnvironmentVariables = environmentVariables);

        public static Job With(this Job job, IReadOnlyList<Argument> arguments) => job.WithCore(j => j.Infrastructure.Arguments = arguments);
        
        /// <summary>
        /// Runs the job with a specific NuGet dependency which will be resolved during the Job build process
        /// </summary>
        /// <param name="job"></param>
        /// <param name="packageName">The NuGet package name</param>
        /// <param name="packageVersion">The NuGet package version</param>
        /// <returns></returns>
        public static Job WithNuGet(this Job job, string packageName, string packageVersion) => job.WithCore(j => j.Infrastructure.NuGetReferences = new NuGetReferenceList(j.Infrastructure.NuGetReferences ?? Array.Empty<NuGetReference>()) { new NuGetReference(packageName, packageVersion) });

        /// <summary>
        /// Runs the job with a specific NuGet dependency which will be resolved during the Job build process
        /// </summary>
        /// <param name="job"></param>
        /// <param name="packageName">The NuGet package name, the latest version will be resolved</param>
        /// <returns></returns>
        public static Job WithNuGet(this Job job, string packageName) => job.WithNuGet(packageName, string.Empty);

        /// <summary>
        /// Runs the job with a specific NuGet dependencies which will be resolved during the Job build process
        /// </summary>
        /// <param name="job"></param>
        /// <param name="nuGetReferences">A collection of NuGet dependencies</param>
        /// <returns></returns>
        public static Job WithNuGet(this Job job, IReadOnlyCollection<NuGetReference> nuGetReferences) => job.WithCore(j => j.Infrastructure.NuGetReferences = nuGetReferences);

        // Accuracy
        /// <summary>
        /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
        /// The default value is 0.02.
        /// <remarks>If <see cref="AccuracyMode.MaxAbsoluteError"/> is also provided, the smallest value is used as stop criteria.</remarks>
        /// </summary>
        public static Job WithMaxRelativeError(this Job job, double value) => job.WithCore(j => j.Accuracy.MaxRelativeError = value);

        /// <summary>
        /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
        /// Doesn't have a default value.
        /// <remarks>If <see cref="AccuracyMode.MaxRelativeError"/> is also provided, the smallest value is used as stop criteria.</remarks>
        /// </summary>
        public static Job WithMaxAbsoluteError(this Job job, TimeInterval value) => job.WithCore(j => j.Accuracy.MaxAbsoluteError = value);

        /// <summary>
        /// Minimum time of a single iteration. Unlike Run.IterationTime, this characteristic specifies only the lower limit. In case of need, BenchmarkDotNet can increase this value.
        /// The default value is 500 milliseconds.
        /// </summary>
        public static Job WithMinIterationTime(this Job job, TimeInterval value) => job.WithCore(j => j.Accuracy.MinIterationTime = value);

        /// <summary>
        /// Minimum count of benchmark invocations per iteration
        /// The default value is 4.
        /// </summary>
        public static Job WithMinInvokeCount(this Job job, int value) => job.WithCore(j => j.Accuracy.MinInvokeCount = value);

        /// <summary>
        /// Specifies if the overhead should be evaluated (Idle runs) and it's average value subtracted from every result.
        /// True by default, very important for nano-benchmarks.
        /// </summary>
        public static Job WithEvaluateOverhead(this Job job, bool value) => job.WithCore(j => j.Accuracy.EvaluateOverhead = value);

        /// <summary>
        /// Specifies which outliers should be removed from the distribution
        /// </summary>
        public static Job WithOutlierMode(this Job job, OutlierMode value) => job.WithCore(j => j.Accuracy.OutlierMode = value);

        [Obsolete("Please use the new WithOutlierMode instead")]
        [PublicAPI]
        public static Job WithRemoveOutliers(this Job job, bool value) =>
            job.WithCore(j => j.Accuracy.OutlierMode = value ? OutlierMode.OnlyUpper : OutlierMode.None);

        [PublicAPI]
        public static Job WithAnalyzeLaunchVariance(this Job job, bool value) => job.WithCore(j => j.Accuracy.AnalyzeLaunchVariance = value);

        // Meta
        public static Job AsBaseline(this Job job) => job.WithCore(j => j.Meta.Baseline = true);
        public static Job WithBaseline(this Job job, bool value) => job.WithCore(j => j.Meta.Baseline = value);

        [Obsolete("Please use the new WithBaseline instead"), PublicAPI]
        public static Job WithIsBaseline(this Job job, bool value) => value ? job.AsBaseline() : job;

        /// <summary>
        /// mutator job should not be added to the config, but instead applied to other jobs in given config
        /// </summary>
        public static Job AsMutator(this Job job) => job.WithCore(j => j.Meta.IsMutator = true);
        
        /// <summary>
        /// use it if you want to specify custom default settings for default job used by console arguments parser
        /// </summary>
        public static Job AsDefault(this Job job, bool value = true) => job.WithCore(j => j.Meta.IsDefault = value);

        internal static Job MakeSettingsUserFriendly(this Job job, Descriptor descriptor)
        {
            // users expect that if IterationSetup is configured, it should be run before every benchmark invocation https://github.com/dotnet/BenchmarkDotNet/issues/730
            if ((descriptor.IterationSetupMethod != null || descriptor.IterationCleanupMethod != null)
                && !job.HasValue(RunMode.InvocationCountCharacteristic)
                && !job.HasValue(RunMode.UnrollFactorCharacteristic))
            {
                return job.RunOncePerIteration();
            }

            return job;
        }

        private static Job WithCore(this Job job, Action<Job> updateCallback)
        {
            bool hasId = job.HasValue(Job.IdCharacteristic);

            var newJob = hasId ? new Job(job.Id, job) : new Job(job);
            updateCallback(newJob);
            return newJob;
        }
    }
}