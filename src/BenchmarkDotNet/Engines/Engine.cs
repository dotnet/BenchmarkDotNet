using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    [UsedImplicitly]
    public class Engine : IEngine
    {
        internal EngineParameters Parameters { get; }

        private IClock Clock { get; }
        private bool ForceGcCleanups { get; }
        private bool MemoryRandomization { get; }

        private readonly Random random;

        private IHost Host => Parameters.Host;
        private Job TargetJob => Parameters.TargetJob;
        private IResolver Resolver => Parameters.Resolver;

        internal Engine(EngineParameters engineParameters)
        {
            if (engineParameters == null) throw new ArgumentNullException(nameof(engineParameters));

            // EngineParameters properties are mutable, so we copy/freeze them all.
            var job = engineParameters.TargetJob ?? throw new ArgumentNullException(nameof(EngineParameters.TargetJob));
            Parameters = new()
            {
                WorkloadActionNoUnroll = engineParameters.WorkloadActionNoUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.WorkloadActionNoUnroll)),
                WorkloadActionUnroll = engineParameters.WorkloadActionUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.WorkloadActionUnroll)),
                Dummy1Action = engineParameters.Dummy1Action ?? throw new ArgumentNullException(nameof(EngineParameters.Dummy1Action)),
                Dummy2Action = engineParameters.Dummy2Action ?? throw new ArgumentNullException(nameof(EngineParameters.Dummy2Action)),
                Dummy3Action = engineParameters.Dummy3Action ?? throw new ArgumentNullException(nameof(EngineParameters.Dummy3Action)),
                OverheadActionNoUnroll = engineParameters.OverheadActionNoUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.OverheadActionNoUnroll)),
                OverheadActionUnroll = engineParameters.OverheadActionUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.OverheadActionUnroll)),
                GlobalSetupAction = engineParameters.GlobalSetupAction ?? throw new ArgumentNullException(nameof(EngineParameters.GlobalSetupAction)),
                GlobalCleanupAction = engineParameters.GlobalCleanupAction ?? throw new ArgumentNullException(nameof(EngineParameters.GlobalCleanupAction)),
                IterationSetupAction = engineParameters.IterationSetupAction ?? throw new ArgumentNullException(nameof(EngineParameters.IterationSetupAction)),
                IterationCleanupAction = engineParameters.IterationCleanupAction ?? throw new ArgumentNullException(nameof(EngineParameters.IterationCleanupAction)),
                TargetJob = new Job(job).Freeze(),
                BenchmarkName = engineParameters.BenchmarkName,
                MeasureExtraStats = engineParameters.MeasureExtraStats,
                Host = engineParameters.Host,
                OperationsPerInvoke = engineParameters.OperationsPerInvoke,
                Resolver = engineParameters.Resolver
            };

            Clock = TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceGcCleanups = TargetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            MemoryRandomization = TargetJob.ResolveValue(RunMode.MemoryRandomizationCharacteristic, Resolver);

            random = new Random(12345); // we are using constant seed to try to get repeatable results
        }

        public void Dispose()
        {
            try
            {
                Parameters.GlobalCleanupAction.Invoke();
            }
            catch (Exception e)
            {
                Host.SendError("Exception during GlobalCleanup!");
                Host.SendError(e.Message);

                // we don't rethrow because this code is executed in a finally block
                // and it could possibly overwrite current exception #1045
            }
        }

        // AggressiveOptimization forces the method to go straight to tier1 JIT, and will never be re-jitted,
        // eliminating tiered JIT as a potential variable in measurements.
        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        public RunResults Run()
        {
            var measurements = new List<Measurement>();

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(Parameters.BenchmarkName);

            IterationData extraStatsIterationData = default;
            // Enumerate the stages and run iterations in a loop to ensure each benchmark invocation is called with a constant stack size.
            // #1120
            foreach (var stage in EngineStage.EnumerateStages(Parameters))
            {
                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.BeforeMainRun();
                }

                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = RunIteration(iterationData);
                    if (iterationData.mode != IterationMode.Dummy)
                    {
                        stageMeasurements.Add(measurement);
                        // Actual Workload is always the last stage, so we use the same data to run extra stats.
                        extraStatsIterationData = iterationData;
                    }
                }
                measurements.AddRange(stageMeasurements);

                Host.WriteLine();

                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.AfterMainRun();
                }
            }

            (GcStats workGcHasDone, ThreadingStats threadingStats, double exceptionFrequency) = Parameters.MeasureExtraStats
                ? GetExtraStats(extraStatsIterationData)
                : default;

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStop(Parameters.BenchmarkName);

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(measurements, outlierMode, workGcHasDone, threadingStats, exceptionFrequency);
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.invokeCount;
            int unrollFactor = data.unrollFactor;
            if (invokeCount % unrollFactor != 0)
                throw new ArgumentOutOfRangeException(nameof(data), $"InvokeCount({invokeCount}) should be a multiple of UnrollFactor({unrollFactor}).");

            long totalOperations = invokeCount * Parameters.OperationsPerInvoke;
            bool randomizeMemory = data.mode == IterationMode.Workload && MemoryRandomization;

            data.setupAction();

            GcCollect();

            if (EngineEventSource.Log.IsEnabled() && data.mode != IterationMode.Dummy)
                EngineEventSource.Log.IterationStart(data.mode, data.stage, totalOperations);

            var clockSpan = randomizeMemory
                ? MeasureWithRandomStack(data.workloadAction, invokeCount / unrollFactor)
                : Measure(data.workloadAction, invokeCount / unrollFactor);

            if (EngineEventSource.Log.IsEnabled() && data.mode != IterationMode.Dummy)
                EngineEventSource.Log.IterationStop(data.mode, data.stage, totalOperations);

            data.cleanupAction();

            if (randomizeMemory)
                RandomizeManagedHeapMemory();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.mode, data.stage, data.index, totalOperations, clockSpan.GetNanoseconds());
            if (data.mode != IterationMode.Dummy)
            {
                Host.WriteLine(measurement.ToString());
            }
            return measurement;
        }

        // This is in a separate method, because stackalloc can affect code alignment,
        // resulting in unexpected measurements on some AMD cpus,
        // even if the stackalloc branch isn't executed. (#2366)
        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private unsafe ClockSpan MeasureWithRandomStack(Action<long> action, long invokeCount)
        {
            byte* stackMemory = stackalloc byte[random.Next(32)];
            var clockSpan = Measure(action, invokeCount);
            Consume(stackMemory);
            return clockSpan;
        }

        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private unsafe void Consume(byte* _) { }

        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private ClockSpan Measure(Action<long> action, long invokeCount)
        {
            var clock = Clock.Start();
            action(invokeCount);
            return clock.GetElapsed();
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private (GcStats, ThreadingStats, double) GetExtraStats(IterationData data)
        {
            // Warm up the measurement functions before starting the actual measurement.
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadFinal());

            data.setupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            var initialThreadingStats = ThreadingStats.ReadInitial(); // this method might allocate
            var exceptionsStats = new ExceptionsStats(); // allocates
            exceptionsStats.StartListening(); // this method might allocate

            // GC collect before measuring allocations.
            ForceGcCollect();

            // #1542
            // If the jit is tiered, we put the current thread to sleep so it can kick in, compile its stuff,
            // and NOT allocate anything on the background thread when we are measuring allocations.
            SleepIfPositive(JitInfo.BackgroundCompilationDelay);

            GcStats gcStats;
            using (FinalizerBlocker.MaybeStart())
            {
                gcStats = MeasureWithGc(data.workloadAction, data.invokeCount / data.unrollFactor);
            }

            exceptionsStats.Stop(); // this method might (de)allocate
            var finalThreadingStats = ThreadingStats.ReadFinal();

            data.cleanupAction(); // we run iteration cleanup after collecting GC stats

            var totalOperationsCount = data.invokeCount * Parameters.OperationsPerInvoke;
            return (gcStats.WithTotalOperations(totalOperationsCount),
                (finalThreadingStats - initialThreadingStats).WithTotalOperations(totalOperationsCount),
                exceptionsStats.ExceptionsCount / (double)totalOperationsCount);
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        internal static void SleepIfPositive(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero)
            {
                Thread.Sleep(timeSpan);
            }
        }

        // Isolate the allocation measurement and skip tier0 jit to make sure we don't get any unexpected allocations.
        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private GcStats MeasureWithGc(Action<long> action, long invokeCount)
        {
            var initialGcStats = GcStats.ReadInitial();
            action(invokeCount);
            var finalGcStats = GcStats.ReadFinal();
            return finalGcStats - initialGcStats;
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private void RandomizeManagedHeapMemory()
        {
            // invoke global cleanup before global setup
            Parameters.GlobalCleanupAction.Invoke();

            var gen0object = new byte[random.Next(32)];
            var lohObject = new byte[85 * 1024 + random.Next(32)];

            // we expect the key allocations to happen in global setup (not ctor)
            // so we call it while keeping the random-size objects alive
            Parameters.GlobalSetupAction.Invoke();

            GC.KeepAlive(gen0object);
            GC.KeepAlive(lohObject);

            // we don't enforce GC.Collects here as engine does it later anyway
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private void GcCollect()
        {
            if (!ForceGcCleanups)
                return;

            ForceGcCollect();
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        internal static void ForceGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [UsedImplicitly]
        public static class Signals
        {
            public const string Acknowledgment = "Acknowledgment";

            private static readonly Dictionary<HostSignal, string> SignalsToMessages
                = new Dictionary<HostSignal, string>
                {
                    { HostSignal.BeforeAnythingElse, "// BeforeAnythingElse" },
                    { HostSignal.BeforeActualRun, "// BeforeActualRun" },
                    { HostSignal.AfterActualRun, "// AfterActualRun" },
                    { HostSignal.AfterAll, "// AfterAll" }
                };

            private static readonly Dictionary<string, HostSignal> MessagesToSignals
                = SignalsToMessages.ToDictionary(p => p.Value, p => p.Key);

            public static string ToMessage(HostSignal signal) => SignalsToMessages[signal];

            public static bool TryGetSignal(string message, out HostSignal signal)
                => MessagesToSignals.TryGetValue(message, out signal);
        }

        // Very long key and value so this shouldn't be used outside of unit tests.
        internal const string UnitTestBlockFinalizerEnvKey = "BENCHMARKDOTNET_UNITTEST_BLOCK_FINALIZER_FOR_MEMORYDIAGNOSER";
        internal const string UnitTestBlockFinalizerEnvValue = UnitTestBlockFinalizerEnvKey + "_ACTIVE";

        // To prevent finalizers interfering with allocation measurements for unit tests,
        // we block the finalizer thread until we've completed the measurement.
        // https://github.com/dotnet/runtime/issues/101536#issuecomment-2077647417
        private readonly struct FinalizerBlocker : IDisposable
        {
            private readonly object hangLock;

            private FinalizerBlocker(object hangLock) => this.hangLock = hangLock;

            private sealed class Impl
            {
                // ManualResetEvent(Slim) allocates when it is waited and yields the thread,
                // so we use Monitor.Wait instead which does not allocate managed memory.
                // This behavior is not documented, but was observed with the VS Profiler.
                private readonly object hangLock = new();
                private readonly ManualResetEventSlim enteredFinalizerEvent = new(false);

                [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
                ~Impl()
                {
                    lock (hangLock)
                    {
                        enteredFinalizerEvent.Set();
                        Monitor.Wait(hangLock);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
                internal static (object hangLock, ManualResetEventSlim enteredFinalizerEvent) CreateWeakly()
                {
                    var impl = new Impl();
                    return (impl.hangLock, impl.enteredFinalizerEvent);
                }
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            internal static FinalizerBlocker MaybeStart()
            {
                if (Environment.GetEnvironmentVariable(UnitTestBlockFinalizerEnvKey) != UnitTestBlockFinalizerEnvValue)
                {
                    return default;
                }
                var (hangLock, enteredFinalizerEvent) = Impl.CreateWeakly();
                do
                {
                    GC.Collect();
                    // Do NOT call GC.WaitForPendingFinalizers.
                }
                while (!enteredFinalizerEvent.IsSet);
                return new FinalizerBlocker(hangLock);
            }

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            public void Dispose()
            {
                if (hangLock is not null)
                {
                    lock (hangLock)
                    {
                        Monitor.Pulse(hangLock);
                    }
                }
            }
        }
    }
}