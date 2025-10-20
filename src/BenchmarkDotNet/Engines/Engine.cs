using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    [UsedImplicitly]
    public class Engine : IEngine
    {
        [PublicAPI] public IHost Host { get; }
        [PublicAPI] public Action<long> WorkloadAction { get; }
        [PublicAPI] public Action Dummy1Action { get; }
        [PublicAPI] public Action Dummy2Action { get; }
        [PublicAPI] public Action Dummy3Action { get; }
        [PublicAPI] public Action<long> OverheadAction { get; }
        [PublicAPI] public Job TargetJob { get; }
        [PublicAPI] public long OperationsPerInvoke { get; }
        [PublicAPI] public Action GlobalSetupAction { get; }
        [PublicAPI] public Action GlobalCleanupAction { get; }
        [PublicAPI] public Action IterationSetupAction { get; }
        [PublicAPI] public Action IterationCleanupAction { get; }
        [PublicAPI] public IResolver Resolver { get; }
        [PublicAPI] public CultureInfo CultureInfo { get; }
        [PublicAPI] public string BenchmarkName { get; }

        private IClock Clock { get; }
        private bool ForceGcCleanups { get; }
        private int UnrollFactor { get; }
        private RunStrategy Strategy { get; }
        private bool EvaluateOverhead { get; }
        private bool MemoryRandomization { get; }

        private readonly List<Measurement> jittingMeasurements = new(10);
        private readonly bool includeExtraStats;
        private readonly Random random;
        private readonly Diagnosers.CompositeInProcessDiagnoserHandler inProcessDiagnoserHandler;

        internal Engine(
            IHost host,
            IResolver resolver,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> overheadAction, Action<long> workloadAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke,
            bool includeExtraStats, string benchmarkName, Diagnosers.CompositeInProcessDiagnoserHandler inProcessDiagnoserHandler)
        {

            Host = host;
            OverheadAction = overheadAction;
            Dummy1Action = dummy1Action;
            Dummy2Action = dummy2Action;
            Dummy3Action = dummy3Action;
            WorkloadAction = workloadAction;
            TargetJob = targetJob;
            GlobalSetupAction = globalSetupAction;
            GlobalCleanupAction = globalCleanupAction;
            IterationSetupAction = iterationSetupAction;
            IterationCleanupAction = iterationCleanupAction;
            OperationsPerInvoke = operationsPerInvoke;
            this.includeExtraStats = includeExtraStats;
            BenchmarkName = benchmarkName;
            this.inProcessDiagnoserHandler = inProcessDiagnoserHandler;

            Resolver = resolver;

            Clock = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceGcCleanups = targetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            UnrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, Resolver);
            Strategy = targetJob.ResolveValue(RunMode.RunStrategyCharacteristic, Resolver);
            EvaluateOverhead = targetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, Resolver);
            MemoryRandomization = targetJob.ResolveValue(RunMode.MemoryRandomizationCharacteristic, Resolver);

            random = new Random(12345); // we are using constant seed to try to get repeatable results
        }

        public void Dispose()
        {
            try
            {
                GlobalCleanupAction?.Invoke();
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
            measurements.AddRange(jittingMeasurements);

            long invokeCount = TargetJob.ResolveValue(RunMode.InvocationCountCharacteristic, Resolver, 1);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(BenchmarkName);

            // Enumerate the stages and run iterations in a loop to ensure each benchmark invocation is called with a constant stack size.
            // #1120
            foreach (var stage in EngineStage.EnumerateStages(this, Strategy, EvaluateOverhead))
            {
                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.BeforeMainRun();
                    inProcessDiagnoserHandler.Handle(BenchmarkSignal.BeforeActualRun);
                }

                var stageMeasurements = stage.GetMeasurementList();
                // 1-based iterationIndex
                int iterationIndex = 1;
                while (stage.GetShouldRunIteration(stageMeasurements, ref invokeCount))
                {
                    var measurement = RunIteration(new IterationData(stage.Mode, stage.Stage, iterationIndex, invokeCount, UnrollFactor));
                    stageMeasurements.Add(measurement);
                    ++iterationIndex;
                }
                measurements.AddRange(stageMeasurements);

                WriteLine();

                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    Host.AfterMainRun();
                    inProcessDiagnoserHandler.Handle(BenchmarkSignal.AfterActualRun);
                }
            }

            (GcStats workGcHasDone, ThreadingStats threadingStats, double exceptionFrequency) = includeExtraStats
                ? GetExtraStats(new IterationData(IterationMode.Workload, IterationStage.Actual, 0, invokeCount, UnrollFactor))
                : (GcStats.Empty, ThreadingStats.Empty, 0);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStop(BenchmarkName);

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(measurements, outlierMode, workGcHasDone, threadingStats, exceptionFrequency);
        }

        [MethodImpl(CodeGenHelper.AggressiveOptimizationOption)]
        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            if (invokeCount % unrollFactor != 0)
                throw new ArgumentOutOfRangeException(nameof(data), $"InvokeCount({invokeCount}) should be a multiple of UnrollFactor({unrollFactor}).");

            long totalOperations = invokeCount * OperationsPerInvoke;
            bool isOverhead = data.IterationMode == IterationMode.Overhead;
            bool randomizeMemory = !isOverhead && MemoryRandomization;
            var action = isOverhead ? OverheadAction : WorkloadAction;

            if (!isOverhead)
                IterationSetupAction();

            GcCollect();

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStart(data.IterationMode, data.IterationStage, totalOperations);

            var clockSpan = randomizeMemory
                ? MeasureWithRandomStack(action, invokeCount / unrollFactor)
                : Measure(action, invokeCount / unrollFactor);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStop(data.IterationMode, data.IterationStage, totalOperations);

            if (!isOverhead)
                IterationCleanupAction();

            if (randomizeMemory)
                RandomizeManagedHeapMemory();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.IterationStage, data.Index, totalOperations, clockSpan.GetNanoseconds());
            WriteLine(measurement.ToString());
            if (measurement.IterationStage == IterationStage.Jitting)
                jittingMeasurements.Add(measurement);

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void Consume(byte* _) { }

        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private ClockSpan Measure(Action<long> action, long invokeCount)
        {
            var clock = Clock.Start();
            action(invokeCount);
            return clock.GetElapsed();
        }

        private (GcStats, ThreadingStats, double) GetExtraStats(IterationData data)
        {
            // Warm up the measurement functions before starting the actual measurement.
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadFinal());

            IterationSetupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            var initialThreadingStats = ThreadingStats.ReadInitial(); // this method might allocate
            var exceptionsStats = new ExceptionsStats(); // allocates
            exceptionsStats.StartListening(); // this method might allocate

#if !NET7_0_OR_GREATER
            if (RuntimeInformation.IsNetCore && Environment.Version.Major is >= 3 and <= 6 && RuntimeInformation.IsTieredJitEnabled)
            {
                // #1542
                // We put the current thread to sleep so tiered jit can kick in, compile its stuff,
                // and NOT allocate anything on the background thread when we are measuring allocations.
                // This is only an issue on netcoreapp3.0 to net6.0. Tiered jit allocations were "fixed" in net7.0
                // (maybe not completely eliminated forever, but at least reduced to a point where measurements are much more stable),
                // and netcoreapp2.X uses only GetAllocatedBytesForCurrentThread which doesn't capture the tiered jit allocations.
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
#endif

            // GC collect before measuring allocations.
            ForceGcCollect();
            GcStats gcStats;
            using (FinalizerBlocker.MaybeStart())
            {
                gcStats = MeasureWithGc(data.InvokeCount / data.UnrollFactor);
            }

            exceptionsStats.Stop(); // this method might (de)allocate
            var finalThreadingStats = ThreadingStats.ReadFinal();

            IterationCleanupAction(); // we run iteration cleanup after collecting GC stats

            var totalOperationsCount = data.InvokeCount * OperationsPerInvoke;
            return (gcStats.WithTotalOperations(totalOperationsCount),
                (finalThreadingStats - initialThreadingStats).WithTotalOperations(totalOperationsCount),
                exceptionsStats.ExceptionsCount / (double)totalOperationsCount);
        }

        // Isolate the allocation measurement and skip tier0 jit to make sure we don't get any unexpected allocations.
        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private GcStats MeasureWithGc(long invokeCount)
        {
            var initialGcStats = GcStats.ReadInitial();
            WorkloadAction(invokeCount);
            var finalGcStats = GcStats.ReadFinal();
            return finalGcStats - initialGcStats;
        }

        private void RandomizeManagedHeapMemory()
        {
            // invoke global cleanup before global setup
            GlobalCleanupAction?.Invoke();

            var gen0object = new byte[random.Next(32)];
            var lohObject = new byte[85 * 1024 + random.Next(32)];

            // we expect the key allocations to happen in global setup (not ctor)
            // so we call it while keeping the random-size objects alive
            GlobalSetupAction?.Invoke();

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

        public void WriteLine(string text) => Host.WriteLine(text);

        public void WriteLine() => Host.WriteLine();

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