using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
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

        public Job TargetJob { get; set; } = Job.Default;
        public long OperationsPerInvoke { get; set; } = 1;
        public Action SetupAction { get; set; } = null;
        public Action CleanupAction { get; set; } = null;
        public bool IsDiagnoserAttached { get; set; }
        public Action<long> MainAction { get; }
        public Action<long> IdleAction { get; }
        public IResolver Resolver { get; }

        private bool ForceAllocations { get; set; }
        private IClock Clock { get; set; }
        private int UnrollFactor { get; set; }
        private RunStrategy Strategy { get; set; }
        private bool EvaluateOverhead { get; set; }

        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineTargetStage targetStage;

        public Engine([NotNull] Action<long> idleAction, [NotNull] Action<long> mainAction)
        {
            IdleAction = idleAction;
            MainAction = mainAction;
            pilotStage = new EnginePilotStage(this);
            warmupStage = new EngineWarmupStage(this);
            targetStage = new EngineTargetStage(this);
            Resolver = new CompositeResolver(BenchmarkRunnerCore.DefaultResolver, EngineResolver.Instance);
        }

        /// <summary>
        /// allocation-heavy method, execute before attaching any diagnosers!
        /// </summary>
        public void Initialize()
        {
            ForceAllocations = TargetJob.Env.Gc.Force.Resolve(Resolver);
            Clock = TargetJob.Infrastructure.Clock.Resolve(Resolver);
            UnrollFactor = TargetJob.Run.UnrollFactor.Resolve(Resolver);
            Strategy = TargetJob.Run.RunStrategy.Resolve(Resolver);
            EvaluateOverhead = TargetJob.Accuracy.EvaluateOverhead.Resolve(Resolver);

            targetStage.PreAllocateResultsList(TargetJob.Run.TargetCount);

            if (TimeUnit.All == null) { throw new Exception("just use this type here to provoke static ctor"); }
        }

        public RunResults Run()
        {
            Jitting();

            long invokeCount = 1;
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

            var main = targetStage.RunMain(invokeCount, UnrollFactor);

            return new RunResults(idle, main);
        }

        private void Jitting()
        {
            // first signal about jitting is raised from auto-generated Program.cs, look at BenchmarkProgram.txt

            MainAction.Invoke(1);
            IdleAction.Invoke(1);
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

        private static void ForceGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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