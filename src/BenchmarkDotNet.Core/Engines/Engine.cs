using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
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
        public Action<long> MainAction { get; }
        public Action<long> IdleAction { get; }

        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineTargetStage targetStage;

        public IResolver Resolver { get; }

        public Engine([NotNull] Action<long> idleAction, [NotNull] Action<long> mainAction)
        {
            IdleAction = idleAction;
            MainAction = mainAction;
            pilotStage = new EnginePilotStage(this);
            warmupStage = new EngineWarmupStage(this);
            targetStage = new EngineTargetStage(this);
            Resolver = new CompositeResolver(BenchmarkRunnerCore.DefaultResolver, EngineResolver.Instance);
        }

        // TODO: return all measurements
        public void Run()
        {
            Jitting();

            long invokeCount = 1;
            int unrollFactor = TargetJob.Run.UnrollFactor.Resolve(Resolver);
            IList<Measurement> idle = null;

            if (TargetJob.Run.RunStrategy.Resolve(Resolver) != RunStrategy.ColdStart)
            {
                invokeCount = pilotStage.Run();

                if (TargetJob.Accuracy.EvaluateOverhead.Resolve(Resolver))
                {
                    warmupStage.RunIdle(invokeCount, unrollFactor);
                    idle = targetStage.RunIdle(invokeCount, unrollFactor);
                }

                warmupStage.RunMain(invokeCount, unrollFactor);
            }

            var main = targetStage.RunMain(invokeCount, unrollFactor);

            // TODO: Move calculation of the result measurements to a separated class
            PrintResult(idle, main);
        }

        private void Jitting()
        {
            // first signal about jitting is raised from auto-generated Program.cs, look at BenchmarkProgram.txt
            
            MainAction.Invoke(1);
            IdleAction.Invoke(1);
        }

        private void PrintResult(IList<Measurement> idle, IList<Measurement> main)
        {
            // TODO: use Accuracy.RemoveOutliers
            // TODO: check if resulted measurements are too small (like < 0.1ns)
            double overhead = idle == null ? 0.0 : new Statistics(idle.Select(m => m.Nanoseconds)).Median;
            int resultIndex = 0;
            foreach (var measurement in main)
            {
                var resultMeasurement = new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    Math.Max(0, measurement.Nanoseconds - overhead));
                WriteLine(resultMeasurement.ToOutputLine());
            }
            WriteLine();
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
            var clock = TargetJob.Infrastructure.Clock.Resolve(Resolver).Start();
            action(invokeCount / unrollFactor);
            var clockSpan = clock.Stop();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.Index, totalOperations, clockSpan.GetNanoseconds());
            WriteLine(measurement.ToOutputLine());
            return measurement;
        }

        private void GcCollect() => GcCollect(TargetJob.Env.Gc.Force.Resolve(Resolver));

        private static void GcCollect(bool isForce)
        {
            if (!isForce)
                return;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void WriteLine() => Console.WriteLine();
        public void WriteLine(string line) => Console.WriteLine(line);

        [UsedImplicitly]
        public class Signals
        {
            public const string BeforeAnythingElse = "// BeforeAnythingElse";

            public const string AfterSetup = "// AfterSetup";

            public const string BeforeCleanup = "// BeforeCleanup";
        }
    }
}