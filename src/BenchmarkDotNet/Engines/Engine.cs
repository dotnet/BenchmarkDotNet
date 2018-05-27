using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    [UsedImplicitly]
    public class Engine : IEngine
    {
        public const int MinInvokeCount = 4;

        public IHost Host { get; }
        public Action<long> MainAction { get; }
        public Action Dummy1Action { get; }
        public Action Dummy2Action { get; }
        public Action Dummy3Action { get; }
        public Action<long> IdleAction { get; }
        public Job TargetJob { get; }
        public long OperationsPerInvoke { get; }
        public Action GlobalSetupAction { get; }
        public Action GlobalCleanupAction { get; }
        public Action IterationSetupAction { get; }
        public Action IterationCleanupAction { get; }
        public IResolver Resolver { get; }

        private IClock Clock { get; }
        private bool ForceAllocations { get; }
        private int UnrollFactor { get; }
        private RunStrategy Strategy { get; }
        private bool EvaluateOverhead { get; }
        private int InvocationCount { get; }

        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineTargetStage targetStage;
        private readonly bool includeMemoryStats;

        internal Engine(
            IHost host,
            IResolver resolver,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> idleAction, Action<long> mainAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke,
            bool includeMemoryStats)
        {
            
            Host = host;
            IdleAction = idleAction;
            Dummy1Action = dummy1Action;
            Dummy2Action = dummy2Action;
            Dummy3Action = dummy3Action;
            MainAction = mainAction;
            TargetJob = targetJob;
            GlobalSetupAction = globalSetupAction;
            GlobalCleanupAction = globalCleanupAction;
            IterationSetupAction = iterationSetupAction;
            IterationCleanupAction = iterationCleanupAction;
            OperationsPerInvoke = operationsPerInvoke;
            this.includeMemoryStats = includeMemoryStats;

            Resolver = resolver;

            Clock = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceAllocations = targetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            UnrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, Resolver);
            Strategy = targetJob.ResolveValue(RunMode.RunStrategyCharacteristic, Resolver);
            EvaluateOverhead = targetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, Resolver);
            InvocationCount = targetJob.ResolveValue(RunMode.InvocationCountCharacteristic, Resolver);

            warmupStage = new EngineWarmupStage(this);
            pilotStage = new EnginePilotStage(this);
            targetStage = new EngineTargetStage(this);
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
                        warmupStage.RunIdle(invokeCount, UnrollFactor);
                        idle = targetStage.RunIdle(invokeCount, UnrollFactor);
                    }
                }

                warmupStage.RunMain(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring);
            }

            Host.BeforeMainRun();

            var main = targetStage.RunMain(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring);

            Host.AfterMainRun();

            var workGcHasDone = includeMemoryStats 
                ? MeasureGcStats(new IterationData(IterationMode.MainTarget, 0, invokeCount, UnrollFactor)) 
                : GcStats.Empty;

            bool removeOutliers = TargetJob.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, Resolver);

            return new RunResults(idle, main, removeOutliers, workGcHasDone);
        }

        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            long totalOperations = invokeCount * OperationsPerInvoke;
            bool isIdle = data.IterationMode.IsIdle();
            var action = isIdle ? IdleAction : MainAction;

            if(!isIdle)
                IterationSetupAction();

            GcCollect();

            // Measure
            var clock = Clock.Start();
            action(invokeCount / unrollFactor);
            var clockSpan = clock.GetElapsed();

            if(!isIdle)
                IterationCleanupAction();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.Index, totalOperations, clockSpan.GetNanoseconds());
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

            MainAction(data.InvokeCount / data.UnrollFactor);

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

        private void ForceGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void WriteLine(string text) => Host.WriteLine(text);

        public void WriteLine() => Host.WriteLine();

        private void EnableMonitoring()
        {
            if (RuntimeInformation.IsMono) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono
                return;

            if (RuntimeInformation.IsFullFramework)
                AppDomain.MonitoringIsEnabled = true;
        }

        [UsedImplicitly]
        public static class Signals
        {
            public const string DiagnoserIsAttachedParam = "diagnoserAttached";
            public const string Acknowledgment = "Acknowledgment";

            private static readonly Dictionary<HostSignal, string> SignalsToMessages
                = new Dictionary<HostSignal, string>
                {
                    { HostSignal.BeforeAnythingElse, "// BeforeAnythingElse" },
                    { HostSignal.BeforeMainRun, "// BeforeMainRun" },
                    { HostSignal.AfterMainRun, "// AfterMainRun" },
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