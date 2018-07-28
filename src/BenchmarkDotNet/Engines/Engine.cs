using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

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
        [PublicAPI] public Encoding Encoding { get; }
        
        private IClock Clock { get; }
        private bool ForceAllocations { get; }
        private int UnrollFactor { get; }
        private RunStrategy Strategy { get; }
        private bool EvaluateOverhead { get; }
        private int InvocationCount { get; }

        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineActualStage actualStage;
        private readonly bool includeMemoryStats;

        internal Engine(
            IHost host,
            IResolver resolver,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> overheadAction, Action<long> workloadAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke,
            bool includeMemoryStats, Encoding encoding)
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
            this.includeMemoryStats = includeMemoryStats;

            Resolver = resolver;
            Encoding = encoding;

            Clock = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceAllocations = targetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            UnrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, Resolver);
            Strategy = targetJob.ResolveValue(RunMode.RunStrategyCharacteristic, Resolver);
            EvaluateOverhead = targetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, Resolver);
            InvocationCount = targetJob.ResolveValue(RunMode.InvocationCountCharacteristic, Resolver);

            warmupStage = new EngineWarmupStage(this);
            pilotStage = new EnginePilotStage(this);
            actualStage = new EngineActualStage(this);
        }

        public void Dispose() => GlobalCleanupAction?.Invoke();

        public RunResults Run()
        {
            long invokeCount = InvocationCount;
            IReadOnlyList<Measurement> idle = null;

            if (Strategy != RunStrategy.ColdStart)
            {
                if (Strategy != RunStrategy.Monitoring)
                {
                    invokeCount = pilotStage.Run();

                    if (EvaluateOverhead)
                    {
                        warmupStage.RunOverhead(invokeCount, UnrollFactor);
                        idle = actualStage.RunOverhead(invokeCount, UnrollFactor);
                    }
                }

                warmupStage.RunWorkload(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring);
            }

            Host.BeforeMainRun();

            var main = actualStage.RunWorkload(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring);

            Host.AfterMainRun();

            var workGcHasDone = includeMemoryStats 
                ? MeasureGcStats(new IterationData(IterationMode.Workload, IterationStage.Actual, 0, invokeCount, UnrollFactor)) 
                : GcStats.Empty;

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(idle, main, outlierMode, workGcHasDone, Encoding);
        }

        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            long totalOperations = invokeCount * OperationsPerInvoke;
            bool isOverhead = data.IterationMode == IterationMode.Overhead;
            var action = isOverhead ? OverheadAction : WorkloadAction;

            if(!isOverhead)
                IterationSetupAction();

            GcCollect();

            // Measure
            var clock = Clock.Start();
            action(invokeCount / unrollFactor);
            var clockSpan = clock.GetElapsed();

            if(!isOverhead)
                IterationCleanupAction();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.IterationStage, data.Index, totalOperations, clockSpan.GetNanoseconds(), Encoding);
            WriteLine(measurement.ToOutputLine());

            return measurement;
        }

        private GcStats MeasureGcStats(IterationData data)
        {
            // we enable monitoring after main target run, for this single iteration which is executed at the end
            // so even if we enable AppDomain monitoring in separate process
            // it does not matter, because we have already obtained the results!
            EnableMonitoring();

            IterationSetupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            var initialGcStats = GcStats.ReadInitial();

            WorkloadAction(data.InvokeCount / data.UnrollFactor);

            var finalGcStats = GcStats.ReadFinal();

            IterationCleanupAction(); // we run iteration cleanup after collecting GC stats 

            return (finalGcStats - initialGcStats).WithTotalOperations(data.InvokeCount * OperationsPerInvoke);
        }

        private void GcCollect()
        {
            if (!ForceAllocations)
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
            if (RuntimeInformation.IsMono) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono
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