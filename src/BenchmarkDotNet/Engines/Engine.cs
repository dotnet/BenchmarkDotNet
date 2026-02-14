using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

#nullable enable

namespace BenchmarkDotNet.Engines
{
    // MethodImplOptions.AggressiveOptimization is applied to all methods to force them to go straight to tier1 JIT,
    // eliminating tiered JIT as a potential variable in measurements.
    [AggressivelyOptimizeMethods]
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

        public async ValueTask<RunResults> RunAsync()
        {
            await Parameters.GlobalSetupAction.Invoke();
            bool didThrow = false;
            try
            {
                return await RunCore();
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
                    await Parameters.GlobalCleanupAction.Invoke();
                }
                // We only catch if the benchmark threw to not overwrite the exception. #1045
                catch (Exception e) when (didThrow)
                {
                    Host.SendError($"Exception during GlobalCleanup!{Environment.NewLine}{e}");
                }
            }
        }

        // This method is extra long because the helper methods were inlined in order to prevent extra async allocations on each iteration.
        private async ValueTask<RunResults> RunCore()
        {
            var measurements = new List<Measurement>();

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(Parameters.BenchmarkName);

            IterationData extraIterationData = default;
            // Enumerate the stages and run iterations in a loop to ensure each benchmark invocation is called with a constant stack size.
            // #1120
            foreach (var stage in EngineStage.EnumerateStages(Parameters))
            {
                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.BeforeMainRun();
                    await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeActualRun);
                }

                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    // Initialization
                    long invokeCount = iterationData.invokeCount;
                    int unrollFactor = iterationData.unrollFactor;
                    if (invokeCount % unrollFactor != 0)
                        throw new ArgumentOutOfRangeException(nameof(iterationData), $"InvokeCount({invokeCount}) should be a multiple of UnrollFactor({unrollFactor}).");

                    long totalOperations = invokeCount * Parameters.OperationsPerInvoke;
                    bool randomizeMemory = iterationData.mode == IterationMode.Workload && MemoryRandomization;

                    await iterationData.setupAction();

                    GcCollect();

                    if (EngineEventSource.Log.IsEnabled())
                        EngineEventSource.Log.IterationStart(iterationData.mode, iterationData.stage, totalOperations);

                    var clockSpan = randomizeMemory
                        ? await MeasureWithRandomStack(iterationData.workloadAction, invokeCount / unrollFactor)
                        : await iterationData.workloadAction(invokeCount / unrollFactor, Clock);

                    if (EngineEventSource.Log.IsEnabled())
                        EngineEventSource.Log.IterationStop(iterationData.mode, iterationData.stage, totalOperations);

                    await iterationData.cleanupAction();

                    if (randomizeMemory)
                        await RandomizeManagedHeapMemory();

                    GcCollect();

                    // Results
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, totalOperations, clockSpan.GetNanoseconds());
                    Host.WriteLine(measurement.ToString());
                    stageMeasurements.Add(measurement);
                    // Actual Workload is always the last stage, so we use the same data to run extra stats.
                    extraIterationData = iterationData;
                }
                measurements.AddRange(stageMeasurements);

                Host.WriteLine();

                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.AfterMainRun();
                    await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterActualRun);
                }
            }

            GcStats workGcHasDone = default;
            if (Parameters.RunExtraIteration)
            {
                // Warm up the GC measurement functions before starting the actual measurement.
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadFinal());

                await extraIterationData.setupAction!(); // we run iteration setup first, so even if it allocates, it is not included in the results

                Host.SendSignal(HostSignal.BeforeExtraIteration);
                await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeExtraIteration);

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
                    (gcStats, clockSpan) = await MeasureWithGc(extraIterationData.workloadAction!, extraIterationData.invokeCount / extraIterationData.unrollFactor);
                }

                await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterExtraIteration);
                Host.SendSignal(HostSignal.AfterExtraIteration);

                await extraIterationData.cleanupAction!(); // we run iteration cleanup after diagnosers are complete.

                var totalOperations = extraIterationData.invokeCount * Parameters.OperationsPerInvoke;
                var measurement = new Measurement(0, IterationMode.Workload, IterationStage.Extra, 1, totalOperations, clockSpan.GetNanoseconds());
                Host.WriteLine(measurement.ToString());
                workGcHasDone = gcStats.WithTotalOperations(totalOperations);
                measurements.Add(measurement);
            }

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStop(Parameters.BenchmarkName);

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(measurements, outlierMode, workGcHasDone);
        }

        // This is in a separate method, because stackalloc can affect code alignment,
        // resulting in unexpected measurements on some AMD cpus,
        // even if the stackalloc branch isn't executed. (#2366)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe ValueTask<ClockSpan> MeasureWithRandomStack(Func<long, IClock, ValueTask<ClockSpan>> action, long invokeCount)
        {
            byte* stackMemory = stackalloc byte[random.Next(32)];
            var task = action(invokeCount, Clock);
            Consume(stackMemory);
            return task;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void Consume(byte* _) { }

        internal static void SleepIfPositive(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero)
            {
                Thread.Sleep(timeSpan);
            }
        }

        // Isolate the allocation measurement to make sure we don't get any unexpected allocations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async ValueTask<(GcStats, ClockSpan)> MeasureWithGc(Func<long, IClock, ValueTask<ClockSpan>> action, long invokeCount)
        {
            var initialGcStats = GcStats.ReadInitial();
            var clockSpan = await action(invokeCount, Clock);
            var finalGcStats = GcStats.ReadFinal();
            return (finalGcStats - initialGcStats, clockSpan);
        }

        private async ValueTask RandomizeManagedHeapMemory()
        {
            // invoke global cleanup before global setup
            await Parameters.GlobalCleanupAction.Invoke();

            var gen0object = new byte[random.Next(32)];
            var lohObject = new byte[85 * 1024 + random.Next(32)];

            // we expect the key allocations to happen in global setup (not ctor)
            // so we call it while keeping the random-size objects alive
            await Parameters.GlobalSetupAction.Invoke();

            GC.KeepAlive(gen0object);
            GC.KeepAlive(lohObject);

            // we don't enforce GC.Collects here as engine does it later anyway
        }

        private void GcCollect()
        {
            if (!ForceGcCleanups)
                return;

            ForceGcCollect();
        }

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

                ~Impl()
                {
                    lock (hangLock)
                    {
                        enteredFinalizerEvent.Set();
                        Monitor.Wait(hangLock);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static (object hangLock, ManualResetEventSlim enteredFinalizerEvent) CreateWeakly()
                {
                    var impl = new Impl();
                    return (impl.hangLock, impl.enteredFinalizerEvent);
                }
            }

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