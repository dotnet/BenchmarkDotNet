using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public const int MinInvokeCount = 4;

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

        private readonly List<Measurement> jittingMeasurements = new (10);
        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineActualStage actualStage;
        private readonly bool includeExtraStats;
        private readonly Random random;

        internal Engine(
            IHost host,
            IResolver resolver,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> overheadAction, Action<long> workloadAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke,
            bool includeExtraStats, string benchmarkName)
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

            Resolver = resolver;

            Clock = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceGcCleanups = targetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            UnrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, Resolver);
            Strategy = targetJob.ResolveValue(RunMode.RunStrategyCharacteristic, Resolver);
            EvaluateOverhead = targetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, Resolver);
            MemoryRandomization = targetJob.ResolveValue(RunMode.MemoryRandomizationCharacteristic, Resolver);

            warmupStage = new EngineWarmupStage(this);
            pilotStage = new EnginePilotStage(this);
            actualStage = new EngineActualStage(this);

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
            // This method is huge, because all stages are inlined. This ensures the stack size
            // remains constant for each benchmark invocation, eliminating stack sizes as a potential variable in measurements.
            // #1120
            var measurements = new List<Measurement>();
            measurements.AddRange(jittingMeasurements);

            long invokeCount = TargetJob.ResolveValue(RunMode.InvocationCountCharacteristic, Resolver, 1);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(BenchmarkName);

            if (Strategy != RunStrategy.ColdStart)
            {
                if (Strategy != RunStrategy.Monitoring)
                {
                    // Pilot Stage
                    {
                        // If InvocationCount is specified, pilot stage should be skipped
                        if (TargetJob.HasValue(RunMode.InvocationCountCharacteristic))
                        {
                        }
                        // Here we want to guess "perfect" amount of invocation
                        else if (TargetJob.HasValue(RunMode.IterationTimeCharacteristic))
                        {
                            // Perfect invocation count
                            invokeCount = pilotStage.Autocorrect(MinInvokeCount);

                            int iterationCounter = 0;

                            int downCount = 0; // Amount of iterations where newInvokeCount < invokeCount
                            while (true)
                            {
                                iterationCounter++;
                                var measurement = RunIteration(new IterationData(IterationMode.Workload, IterationStage.Pilot, iterationCounter, invokeCount, UnrollFactor));
                                measurements.Add(measurement);
                                double actualIterationTime = measurement.Nanoseconds;
                                long newInvokeCount = pilotStage.Autocorrect(Math.Max(pilotStage.minInvokeCount, (long) Math.Round(invokeCount * pilotStage.targetIterationTime / actualIterationTime)));

                                if (newInvokeCount < invokeCount)
                                    downCount++;

                                if (Math.Abs(newInvokeCount - invokeCount) <= 1 || downCount >= 3)
                                    break;

                                invokeCount = newInvokeCount;
                            }
                            WriteLine();
                        }
                        else
                        {
                            // A case where we don't have specific iteration time.
                            invokeCount = pilotStage.Autocorrect(pilotStage.minInvokeCount);

                            int iterationCounter = 0;
                            while (true)
                            {
                                iterationCounter++;
                                var measurement = RunIteration(new IterationData(IterationMode.Workload, IterationStage.Pilot, iterationCounter, invokeCount, UnrollFactor));
                                measurements.Add(measurement);
                                double iterationTime = measurement.Nanoseconds;
                                double operationError = 2.0 * pilotStage.resolution / invokeCount; // An operation error which has arisen due to the Chronometer precision

                                // Max acceptable operation error
                                double operationMaxError1 = iterationTime / invokeCount * pilotStage.maxRelativeError;
                                double operationMaxError2 = pilotStage.maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                                double operationMaxError = Math.Min(operationMaxError1, operationMaxError2);

                                bool isFinished = operationError < operationMaxError && iterationTime >= pilotStage.minIterationTime.Nanoseconds;
                                if (isFinished)
                                    break;
                                if (invokeCount >= EnginePilotStage.MaxInvokeCount)
                                    break;

                                if (UnrollFactor == 1 && invokeCount < EnvironmentResolver.DefaultUnrollFactorForThroughput)
                                    invokeCount += 1;
                                else
                                    invokeCount *= 2;
                            }
                            WriteLine();
                        }
                    }
                    // End Pilot Stage

                    if (EvaluateOverhead)
                    {
                        // Warmup Overhead
                        {
                            var warmupMeasurements = new List<Measurement>();

                            var criteria = DefaultStoppingCriteriaFactory.Instance.CreateWarmup(TargetJob, Resolver, IterationMode.Overhead, RunStrategy.Throughput);
                            int iterationCounter = 0;
                            while (!criteria.Evaluate(warmupMeasurements).IsFinished)
                            {
                                iterationCounter++;
                                warmupMeasurements.Add(RunIteration(new IterationData(IterationMode.Overhead, IterationStage.Warmup, iterationCounter, invokeCount, UnrollFactor)));
                            }
                            WriteLine();

                            measurements.AddRange(warmupMeasurements);
                        }
                        // End Warmup Overhead

                        // Actual Overhead
                        {
                            var measurementsForStatistics = new List<Measurement>(actualStage.maxIterationCount);

                            int iterationCounter = 0;
                            double effectiveMaxRelativeError = EngineActualStage.MaxOverheadRelativeError;
                            while (true)
                            {
                                iterationCounter++;
                                var measurement = RunIteration(new IterationData(IterationMode.Overhead, IterationStage.Actual, iterationCounter, invokeCount, UnrollFactor));
                                measurements.Add(measurement);
                                measurementsForStatistics.Add(measurement);

                                var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, actualStage.outlierMode);
                                double actualError = statistics.LegacyConfidenceInterval.Margin;

                                double maxError1 = effectiveMaxRelativeError * statistics.Mean;
                                double maxError2 = actualStage.maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                                double maxError = Math.Min(maxError1, maxError2);

                                if (iterationCounter >= actualStage.minIterationCount && actualError < maxError)
                                    break;

                                if (iterationCounter >= actualStage.maxIterationCount || iterationCounter >= EngineActualStage.MaxOverheadIterationCount)
                                    break;
                            }
                            WriteLine();
                        }
                        // End Actual Overhead
                    }
                }

                // Warmup Workload
                {
                    var workloadMeasurements = new List<Measurement>();

                    var criteria = DefaultStoppingCriteriaFactory.Instance.CreateWarmup(TargetJob, Resolver, IterationMode.Workload, Strategy);
                    int iterationCounter = 0;
                    while (!criteria.Evaluate(workloadMeasurements).IsFinished)
                    {
                        iterationCounter++;
                        workloadMeasurements.Add(RunIteration(new IterationData(IterationMode.Workload, IterationStage.Warmup, iterationCounter, invokeCount, UnrollFactor)));
                    }
                    WriteLine();

                    measurements.AddRange(workloadMeasurements);
                }
                // End Warmup Workload
            }

            Host.BeforeMainRun();

            // Actual Workload
            {
                if (actualStage.iterationCount == null && Strategy != RunStrategy.Monitoring)
                {
                    // RunAuto
                    var measurementsForStatistics = new List<Measurement>(actualStage.maxIterationCount);

                    int iterationCounter = 0;
                    double effectiveMaxRelativeError = actualStage.maxRelativeError;
                    while (true)
                    {
                        iterationCounter++;
                        var measurement = RunIteration(new IterationData(IterationMode.Workload, IterationStage.Actual, iterationCounter, invokeCount, UnrollFactor));
                        measurements.Add(measurement);
                        measurementsForStatistics.Add(measurement);

                        var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, actualStage.outlierMode);
                        double actualError = statistics.LegacyConfidenceInterval.Margin;

                        double maxError1 = effectiveMaxRelativeError * statistics.Mean;
                        double maxError2 = actualStage.maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                        double maxError = Math.Min(maxError1, maxError2);

                        if (iterationCounter >= actualStage.minIterationCount && actualError < maxError)
                            break;

                        if (iterationCounter >= actualStage.maxIterationCount)
                            break;
                    }
                }
                else
                {
                    // RunSpecific
                    var iterationCount = actualStage.iterationCount ?? EngineActualStage.DefaultWorkloadCount;
                    for (int i = 0; i < iterationCount; i++)
                        measurements.Add(RunIteration(new IterationData(IterationMode.Workload, IterationStage.Actual, i + 1, invokeCount, UnrollFactor)));
                }
                WriteLine();
            }
            // End Actual Workload

            Host.AfterMainRun();

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
            // we enable monitoring after main target run, for this single iteration which is executed at the end
            // so even if we enable AppDomain monitoring in separate process
            // it does not matter, because we have already obtained the results!
            EnableMonitoring();

            IterationSetupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            var initialThreadingStats = ThreadingStats.ReadInitial(); // this method might allocate
            var exceptionsStats = new ExceptionsStats(); // allocates
            exceptionsStats.StartListening(); // this method might allocate
            var initialGcStats = GcStats.ReadInitial();

            WorkloadAction(data.InvokeCount / data.UnrollFactor);

            exceptionsStats.Stop();
            var finalGcStats = GcStats.ReadFinal();
            var finalThreadingStats = ThreadingStats.ReadFinal();

            IterationCleanupAction(); // we run iteration cleanup after collecting GC stats

            var totalOperationsCount = data.InvokeCount * OperationsPerInvoke;
            GcStats gcStats = (finalGcStats - initialGcStats).WithTotalOperations(totalOperationsCount);
            ThreadingStats threadingStats = (finalThreadingStats - initialThreadingStats).WithTotalOperations(data.InvokeCount * OperationsPerInvoke);

            return (gcStats, threadingStats, exceptionsStats.ExceptionsCount / (double)totalOperationsCount);
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

        private static void ForceGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void WriteLine(string text) => Host.WriteLine(text);

        public void WriteLine() => Host.WriteLine();

        private static void EnableMonitoring()
        {
            if (RuntimeInformation.IsOldMono) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono
                return;

            if (RuntimeInformation.IsFullFramework)
                AppDomain.MonitoringIsEnabled = true;
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
    }
}