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
        public static readonly TimeInterval MinIterationTime = 200 * TimeInterval.Millisecond;

        public Action<long> MainAction { get; }
        public Action<long> IdleAction { get; }
        public Job TargetJob { get; }
        public long OperationsPerInvoke { get; }
        public Action SetupAction { get; }
        public Action CleanupAction { get; }
        public bool IsDiagnoserAttached { get; }
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

        internal Engine(Action<long> idleAction, Action<long> mainAction, Job targetJob, Action setupAction, Action cleanupAction, long operationsPerInvoke, bool isDiagnoserAttached)
        {
            IdleAction = idleAction;
            MainAction = mainAction;
            TargetJob = targetJob;
            SetupAction = setupAction;
            CleanupAction = cleanupAction;
            OperationsPerInvoke = operationsPerInvoke;
            IsDiagnoserAttached = isDiagnoserAttached;

            Resolver = new CompositeResolver(BenchmarkRunnerCore.DefaultResolver, EngineResolver.Instance);

            Clock = targetJob.Infrastructure.Clock.Resolve(Resolver);
            ForceAllocations = targetJob.Env.Gc.Force.Resolve(Resolver);
            UnrollFactor = targetJob.Run.UnrollFactor.Resolve(Resolver);
            Strategy = targetJob.Run.RunStrategy.Resolve(Resolver);
            EvaluateOverhead = targetJob.Accuracy.EvaluateOverhead.Resolve(Resolver);
            InvocationCount = targetJob.Run.InvocationCount.Resolve(Resolver);

            warmupStage = new EngineWarmupStage(this);
            pilotStage = new EnginePilotStage(this);
            targetStage = new EngineTargetStage(this);
        }

        public void PreAllocate()
        {
            var list = new List<Measurement> { new Measurement(), new Measurement() };
            list.Sort(); // provoke JIT, static ctors etc (was allocating 1740 bytes with first call)
            if (TimeUnit.All == null || list[0].Nanoseconds != default(double))
                throw new Exception("just use this things here to provoke static ctor");
            isPreAllocated = true;
        }

        public void Jitting()
        {
            // first signal about jitting is raised from auto-generated Program.cs, look at BenchmarkProgram.txt
            MainAction.Invoke(1);
            IdleAction.Invoke(1);
            isJitted = true;
        }

        public RunResults Run()
        {
            if (!isJitted || !isPreAllocated)
                throw new Exception("You must call PreAllocate() and Jitting() first!");

            long invokeCount = InvocationCount;
            List<Measurement> idle = null;

            if (Strategy != RunStrategy.ColdStart)
            {
                invokeCount = pilotStage.Run();

                if (EvaluateOverhead)
                {
                    warmupStage.RunIdle(invokeCount, UnrollFactor);
                    idle = targetStage.RunIdle(invokeCount, UnrollFactor);
                }

                warmupStage.RunMain(invokeCount, UnrollFactor);
            }

            // we enable monitoring after pilot & warmup, just to ignore the memory allocated by these runs
            EnableMonitoring(); 
            var initialGcStats = GcStats.ReadInitial(IsDiagnoserAttached);

            var main = targetStage.RunMain(invokeCount, UnrollFactor);

            var finalGcStats = GcStats.ReadFinal(IsDiagnoserAttached);
            var forcedCollections = GcStats.FromForced(forcedFullGarbageCollections);
            var workGcHasDone = finalGcStats - forcedCollections - initialGcStats;

            return new RunResults(idle, main, workGcHasDone);
        }

        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            long totalOperations = invokeCount * OperationsPerInvoke;
            var action = data.IterationMode.IsIdle() ? IdleAction : MainAction;

            GcCollect();

            // Measure
            var clock = Clock.Start();
            action(invokeCount / unrollFactor);
            var clockSpan = clock.Stop();

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

            Console.WriteLine(text);
        }

        public void WriteLine()
        {
            EnsureNothingIsPrintedWhenDiagnoserIsAttached();

            Console.WriteLine();
        }

        private void EnableMonitoring()
        {
            if(!IsDiagnoserAttached) // it could affect the results, we do this in separate, diagnostics-only run
                return;
#if CLASSIC
            if(RuntimeInformation.IsMono()) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono
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
            public const string AfterSetup = "// AfterSetup";
            public const string BeforeCleanup = "// BeforeCleanup";
            public const string DiagnoserIsAttachedParam = "diagnoserAttached";
        }
    }
}