using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;
using System.Runtime.CompilerServices;

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
            // EngineParameters properties are mutable, so we copy/freeze them all.
            var job = engineParameters.TargetJob ?? throw new ArgumentNullException(nameof(EngineParameters.TargetJob));
            Parameters = new()
            {
                WorkloadMethods = engineParameters.WorkloadMethods ?? throw new ArgumentNullException(nameof(EngineParameters.WorkloadMethods)),
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

        // ConfigureAwait(true) ensures that user code is executed on the original context.
        public async ValueTask<RunResults> RunAsync()
        {
            Host.CancellationToken.ThrowIfCancellationRequested();

            await Parameters.GlobalSetupAction.Invoke().ConfigureAwait();
            bool didThrowNonCancelation = false;
            try
            {
                return await RunCore().ConfigureAwait();
            }
            catch (Exception e)
            {
                didThrowNonCancelation = !ExceptionHelper.IsProperCancelation(e, Host.CancellationToken);
                throw;
            }
            finally
            {
                try
                {
                    await Parameters.GlobalCleanupAction.Invoke().ConfigureAwait();
                }
                // We only catch if the benchmark threw to not overwrite the exception. #1045
                catch (Exception e) when (didThrowNonCancelation && !ExceptionHelper.IsProperCancelation(e, Host.CancellationToken))
                {
                    Host.SendError($"Exception during GlobalCleanup!{Environment.NewLine}{e}");
                }
            }
        }

        // This method is extra long because the helper methods were inlined in order to prevent extra async allocations on each iteration.
        private async Task<RunResults> RunCore()
        {
            var measurements = new List<Measurement>();

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(Parameters.BenchmarkName);

            IterationData extraIterationData = default;
            // Enumerate the stages and run iterations in a loop to ensure each benchmark invocation is called with a constant stack size. #1120
            foreach (var stage in EngineStage.EnumerateStages(Parameters))
            {
                // Ensure that any resources (like JitListener) are cleaned up.
                using var _ = stage;

                if (stage.Stage == IterationStage.Actual && stage.Mode == IterationMode.Workload)
                {
                    await Host.BeforeMainRunAsync().ConfigureAwait();
                    await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeActualRun, Host.CancellationToken).ConfigureAwait();
                }

                // We need to force an async yield before each stage to ensure each benchmark invocation is called with a constant stack size. #1120
                await AwaitHelper.Yield();
                Host.CancellationToken.ThrowIfCancellationRequested();

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

                    await iterationData.setupAction().ConfigureAwait();
                    bool didThrowNonCancelation = false;
                    ClockSpan clockSpan;
                    try
                    {
                        await YieldAndThrowIfCancellationRequested().ConfigureAwait();

                        GcCollect();

                        if (EngineEventSource.Log.IsEnabled())
                            EngineEventSource.Log.IterationStart(iterationData.mode, iterationData.stage, totalOperations);

                        clockSpan = randomizeMemory
                            ? await MeasureWithRandomStack(iterationData.workloadAction, invokeCount / unrollFactor).ConfigureAwait()
                            : await iterationData.workloadAction(invokeCount / unrollFactor, Clock).ConfigureAwait();

                        await YieldAndThrowIfCancellationRequested().ConfigureAwait();

                        if (EngineEventSource.Log.IsEnabled())
                            EngineEventSource.Log.IterationStop(iterationData.mode, iterationData.stage, totalOperations);
                    }
                    catch (Exception e)
                    {
                        didThrowNonCancelation = !ExceptionHelper.IsProperCancelation(e, Host.CancellationToken);
                        throw;
                    }
                    finally
                    {
                        try
                        {
                            await iterationData.cleanupAction().ConfigureAwait();
                        }
                        // We only catch if the benchmark threw to not overwrite the exception. #1045
                        catch (Exception e) when (didThrowNonCancelation && !ExceptionHelper.IsProperCancelation(e, Host.CancellationToken))
                        {
                            Host.SendError($"Exception during IterationCleanup!{Environment.NewLine}{e}");
                        }
                    }
                    await YieldAndThrowIfCancellationRequested().ConfigureAwait();

                    if (randomizeMemory)
                    {
                        await RandomizeManagedHeapMemory().ConfigureAwait();
                        await YieldAndThrowIfCancellationRequested().ConfigureAwait();
                    }

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
                    await Host.AfterMainRunAsync().ConfigureAwait();
                    await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterActualRun, Host.CancellationToken).ConfigureAwait();
                }
            }

            GcStats workGcHasDone = default;
            if (Parameters.RunExtraIteration)
            {
                // Warm up the GC measurement functions before starting the actual measurement.
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadFinal());

                await extraIterationData.setupAction!().ConfigureAwait(); // we run iteration setup first, so even if it allocates, it is not included in the results

                await Host.SendSignalAsync(HostSignal.BeforeExtraIteration).ConfigureAwait();
                await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeExtraIteration, Host.CancellationToken).ConfigureAwait();

                // GC collect before measuring allocations if the user didn't suppress it.
                GcCollect();

                // #1542
                // If the jit is tiered, we put the current thread to sleep so it can kick in, compile its stuff,
                // and NOT allocate anything on the background thread when we are measuring allocations.
                SleepIfPositive(JitInfo.BackgroundCompilationDelay);

                GcStats gcStats;
                ClockSpan clockSpan;
                try
                {
                    (gcStats, clockSpan) = await MeasureWithGc(extraIterationData.workloadAction!, extraIterationData.invokeCount / extraIterationData.unrollFactor).ConfigureAwait();

                    await Parameters.InProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterExtraIteration, Host.CancellationToken).ConfigureAwait();
                    await Host.SendSignalAsync(HostSignal.AfterExtraIteration).ConfigureAwait();
                }
                finally
                {
                    await extraIterationData.cleanupAction!().ConfigureAwait(); // we run iteration cleanup after diagnosers are complete.
                }

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

        private async ValueTask YieldAndThrowIfCancellationRequested()
        {
            // If this is running in wasm, yield back to JS so that it can detect cancellation.
            await Host.Yield().ConfigureAwait(false);
            Host.CancellationToken.ThrowIfCancellationRequested();
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
            var clockSpan = await action(invokeCount, Clock).ConfigureAwait(false);
            var finalGcStats = GcStats.ReadFinal();
            return (finalGcStats - initialGcStats, clockSpan);
        }

        private async ValueTask RandomizeManagedHeapMemory()
        {
            // invoke global cleanup before global setup
            await Parameters.GlobalCleanupAction.Invoke().ConfigureAwait();

            var gen0object = new byte[random.Next(32)];
            var lohObject = new byte[85 * 1024 + random.Next(32)];

            // we expect the key allocations to happen in global setup (not ctor)
            // so we call it while keeping the random-size objects alive
            await Parameters.GlobalSetupAction.Invoke().ConfigureAwait(false);

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
            public const string Cancel = "CANCEL";

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
    }
}