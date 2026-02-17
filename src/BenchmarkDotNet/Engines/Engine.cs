using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Characteristics;
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
                OverheadActionNoUnroll = engineParameters.OverheadActionNoUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.OverheadActionNoUnroll)),
                OverheadActionUnroll = engineParameters.OverheadActionUnroll ?? throw new ArgumentNullException(nameof(EngineParameters.OverheadActionUnroll)),
                GlobalSetupAction = engineParameters.GlobalSetupAction ?? throw new ArgumentNullException(nameof(EngineParameters.GlobalSetupAction)),
                GlobalCleanupAction = engineParameters.GlobalCleanupAction ?? throw new ArgumentNullException(nameof(EngineParameters.GlobalCleanupAction)),
                IterationSetupAction = engineParameters.IterationSetupAction ?? throw new ArgumentNullException(nameof(EngineParameters.IterationSetupAction)),
                IterationCleanupAction = engineParameters.IterationCleanupAction ?? throw new ArgumentNullException(nameof(EngineParameters.IterationCleanupAction)),
                TargetJob = new Job(job).Freeze(),
                BenchmarkName = engineParameters.BenchmarkName,
                RunExtraIteration = engineParameters.RunExtraIteration,
                Host = engineParameters.Host,
                OperationsPerInvoke = engineParameters.OperationsPerInvoke,
                Resolver = engineParameters.Resolver,
                InProcessDiagnoserHandler = engineParameters.InProcessDiagnoserHandler ?? throw new ArgumentNullException(nameof(EngineParameters.InProcessDiagnoserHandler)),
            };

            Clock = TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver)!;
            ForceGcCleanups = TargetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            MemoryRandomization = TargetJob.ResolveValue(RunMode.MemoryRandomizationCharacteristic, Resolver);

            random = new Random(12345); // we are using constant seed to try to get repeatable results
        }

        public RunResults Run()
        {
            Parameters.GlobalSetupAction.Invoke();
            bool didThrow = false;
            try
            {
                return RunCore();
            }
            catch
            {
                didThrow = true;
                throw;
            }
            finally
            {
                try
                {
                    Parameters.GlobalCleanupAction.Invoke();
                }
                // We only catch if the benchmark threw to not overwrite the exception. #1045
                catch (Exception e) when (didThrow)
                {
                    Host.SendError($"Exception during GlobalCleanup!{Environment.NewLine}{e}");
                }
            }
        }

        // AggressiveOptimization forces the method to go straight to tier1 JIT, and will never be re-jitted,
        // eliminating tiered JIT as a potential variable in measurements.
        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        private RunResults RunCore()
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
                    Parameters.InProcessDiagnoserHandler.Handle(BenchmarkSignal.BeforeActualRun);
                }

                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = RunIteration(iterationData);
                    stageMeasurements.Add(measurement);
                    // Actual Workload is always the last stage, so we use the same data to run extra stats.
                    extraStatsIterationData = iterationData;
                }
                measurements.AddRange(stageMeasurements);

                Host.WriteLine();

                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.AfterMainRun();
                    Parameters.InProcessDiagnoserHandler.Handle(BenchmarkSignal.AfterActualRun);
                }
            }

            GcStats workGcHasDone = default;
            if (Parameters.RunExtraIteration)
            {
                (workGcHasDone, var extraMeasurement) = RunExtraIteration(extraStatsIterationData);
                measurements.Add(extraMeasurement);
            }

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStop(Parameters.BenchmarkName);

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(measurements, outlierMode, workGcHasDone);
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

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStart(data.mode, data.stage, totalOperations);

            var clockSpan = randomizeMemory
                ? MeasureWithRandomStack(data.workloadAction, invokeCount / unrollFactor)
                : Measure(data.workloadAction, invokeCount / unrollFactor);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStop(data.mode, data.stage, totalOperations);

            data.cleanupAction();

            if (randomizeMemory)
                RandomizeManagedHeapMemory();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.mode, data.stage, data.index, totalOperations, clockSpan.GetNanoseconds());
            Host.WriteLine(measurement.ToString());
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
        private (GcStats, Measurement) RunExtraIteration(IterationData data)
        {
            // Warm up the GC measurement functions before starting the actual measurement.
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadFinal());

            data.setupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            Host.SendSignal(HostSignal.BeforeExtraIteration);
            Parameters.InProcessDiagnoserHandler.Handle(BenchmarkSignal.BeforeExtraIteration);

            // GC collect before measuring allocations.
            ForceGcCollect();

            // #1542
            // If the jit is tiered, we put the current thread to sleep so it can kick in, compile its stuff,
            // and NOT allocate anything on the background thread when we are measuring allocations.
            SleepIfPositive(JitInfo.BackgroundCompilationDelay);

            GcStats gcStats;
            ClockSpan clockSpan;
            using (FinalizerBlocker.MaybeStart())
            {
                (gcStats, clockSpan) = MeasureWithGc(data.workloadAction, data.invokeCount / data.unrollFactor);
            }

            Parameters.InProcessDiagnoserHandler.Handle(BenchmarkSignal.AfterExtraIteration);
            Host.SendSignal(HostSignal.AfterExtraIteration);

            data.cleanupAction(); // we run iteration cleanup after diagnosers are complete.

            var totalOperations = data.invokeCount * Parameters.OperationsPerInvoke;
            var measurement = new Measurement(0, IterationMode.Workload, IterationStage.Extra, 1, totalOperations, clockSpan.GetNanoseconds());
            Host.WriteLine(measurement.ToString());
            return (gcStats.WithTotalOperations(totalOperations), measurement);
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
        private (GcStats, ClockSpan) MeasureWithGc(Action<long> action, long invokeCount)
        {
            var initialGcStats = GcStats.ReadInitial();
            var clockSpan = Measure(action, invokeCount);
            var finalGcStats = GcStats.ReadFinal();
            return (finalGcStats - initialGcStats, clockSpan);
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

            private static readonly Dictionary<HostSignal, string> SignalsToMessages = new()
            {
                [HostSignal.BeforeAnythingElse] = "// BeforeAnythingElse",
                [HostSignal.BeforeActualRun] = "// BeforeActualRun",
                [HostSignal.AfterActualRun] = "// AfterActualRun",
                [HostSignal.AfterAll] = "// AfterAll",
                [HostSignal.BeforeExtraIteration] = "// BeforeExtraIteration",
                [HostSignal.AfterExtraIteration] = "// AfterExtraIteration"
            };

            private static readonly Dictionary<string, HostSignal> MessagesToSignals
                = SignalsToMessages.ToDictionary(p => p.Value, p => p.Key);

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
            public static string ToMessage(HostSignal signal) => SignalsToMessages[signal];

            [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
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