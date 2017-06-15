using System;
using System.Collections.Generic;
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
        public bool IsDiagnoserAttached { get; }
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
        private bool isJitted, isPreAllocated;
        private int forcedFullGarbageCollections;

        internal Engine(
            IHost host,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> idleAction, Action<long> mainAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke)
        {
            Host = host;
            IsDiagnoserAttached = host.IsDiagnoserAttached;
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

            Resolver = new CompositeResolver(BenchmarkRunnerCore.DefaultResolver, EngineResolver.Instance);

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

        public void PreAllocate()
        {
            var list = new List<Measurement> { new Measurement(), new Measurement() };
            list.Sort(); // provoke JIT, static ctors etc (was allocating 1740 bytes with first call)
            
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (TimeUnit.All == null || list[0].Nanoseconds != default(double))
                throw new Exception("just use this things here to provoke static ctor");
            isPreAllocated = true;
        }

        public void Jitting()
        {
            // first signal about jitting is raised from auto-generated Program.cs, look at BenchmarkProgram.txt
            Dummy1Action.Invoke();
            MainAction.Invoke(1);
            Dummy2Action.Invoke();
            IdleAction.Invoke(1);
            Dummy3Action.Invoke();
            isJitted = true;
        }

        public RunResults Run()
        {
            if (Strategy.NeedsJitting() != isJitted)
                throw new Exception($"You must{(Strategy.NeedsJitting() ? "" : " not")} call Jitting() first (Strategy = {Strategy})!");
            if (!isPreAllocated)
                throw new Exception("You must call PreAllocate() first!");

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

            // we enable monitoring after pilot & warmup, just to ignore the memory allocated by these runs
            EnableMonitoring();
            if(IsDiagnoserAttached) Host.BeforeMainRun();
            forcedFullGarbageCollections = 0; // zero it in case the Engine instance is reused (InProcessToolchain)
            var initialGcStats = GcStats.ReadInitial(IsDiagnoserAttached);

            var main = targetStage.RunMain(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring);

            var finalGcStats = GcStats.ReadFinal(IsDiagnoserAttached);
            var forcedCollections = GcStats.FromForced(forcedFullGarbageCollections);
            var workGcHasDone = finalGcStats - forcedCollections - initialGcStats;

            bool removeOutliers = TargetJob.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, Resolver);

            return new RunResults(idle, main, removeOutliers, workGcHasDone);
        }

        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            long totalOperations = invokeCount * OperationsPerInvoke;
            var action = data.IterationMode.IsIdle() ? IdleAction : MainAction;

            IterationSetupAction();
            GcCollect();

            // Measure
            var clock = Clock.Start();
            action(invokeCount / unrollFactor);
            var clockSpan = clock.Stop();

            IterationCleanupAction();
            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.Index, totalOperations, clockSpan.GetNanoseconds());
            if (!IsDiagnoserAttached) WriteLine(measurement.ToOutputLine());

            return measurement;
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

            forcedFullGarbageCollections += 2;
        }

        public void WriteLine(string text)
        {
            EnsureNothingIsPrintedWhenDiagnoserIsAttached();

            Host.WriteLine(text);
        }

        public void WriteLine()
        {
            EnsureNothingIsPrintedWhenDiagnoserIsAttached();

            Host.WriteLine();
        }

        private void EnableMonitoring()
        {
            if (!IsDiagnoserAttached) // it could affect the results, we do this in separate, diagnostics-only run
                return;
#if CLASSIC
            if (RuntimeInformation.IsMono()
            ) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono
                return;

            AppDomain.MonitoringIsEnabled = true;
#endif
        }

        private void EnsureNothingIsPrintedWhenDiagnoserIsAttached()
        {
            if (IsDiagnoserAttached)
            {
                throw new InvalidOperationException("to avoid memory allocations we must not print anything when diagnoser is still attached");
            }
        }

        [UsedImplicitly]
        public class Signals
        {
            public const string BeforeAnythingElse = "// BeforeAnythingElse";
            public const string AfterGlobalSetup = "// AfterGlobalSetup";
            public const string BeforeMainRun = "// BeforeMainRun";
            public const string BeforeGlobalCleanup = "// BeforeGlobalCleanup";
            public const string AfterAnythingElse = "// AfterAnythingElse";
            public const string DiagnoserIsAttachedParam = "diagnoserAttached";
        }
    }
}