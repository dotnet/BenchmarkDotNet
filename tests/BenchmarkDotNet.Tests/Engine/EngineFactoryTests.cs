using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineFactoryTests
    {
        private int timesBenchmarkCalled = 0, timesOverheadCalled = 0;
        private int timesGlobalSetupCalled = 0, timesGlobalCleanupCalled = 0, timesIterationSetupCalled = 0, timesIterationCleanupCalled = 0;

        private TimeSpan IterationTime => TimeSpan.FromMilliseconds(EngineResolver.Instance.Resolve(Job.Default, RunMode.IterationTimeCharacteristic).ToMilliseconds());

        private IResolver DefaultResolver => BenchmarkRunnerClean.DefaultResolver;

        private void GlobalSetup() => timesGlobalSetupCalled++;
        private void IterationSetup() => timesIterationSetupCalled++;
        private void IterationCleanup() => timesIterationCleanupCalled++;
        private void GlobalCleanup() => timesGlobalCleanupCalled++;

        private void Throwing(long _) => throw new InvalidOperationException("must NOT be called");

        private void VeryTimeConsumingSingle(long _)
        {
            timesBenchmarkCalled++;
            Thread.Sleep(IterationTime);
        }

        private void TimeConsumingOnlyForTheFirstCall(long _)
        {
            if (timesBenchmarkCalled++ == 0)
            {
                Thread.Sleep(IterationTime);
            }
        }

        private void InstantNoUnroll(long invocationCount) => timesBenchmarkCalled += (int) invocationCount;
        private void InstantUnroll(long _) => timesBenchmarkCalled += 16;

        private void OverheadNoUnroll(long invocationCount) => timesOverheadCalled += (int) invocationCount;
        private void OverheadUnroll(long _) => timesOverheadCalled += 16;

        private static readonly Dictionary<string, Job> JobsWhichDontRequireJitting = new Dictionary<string, Job>
        {
            { "Dry", Job.Dry },
            { "ColdStart", Job.Default.WithStrategy(RunStrategy.ColdStart) },
            { "Monitoring", Job.Default.WithStrategy(RunStrategy.Monitoring) }
        };

        [UsedImplicitly]
        public static TheoryData<string> JobsWhichDontRequireJittingNames => TheoryDataHelper.Create(JobsWhichDontRequireJitting.Keys);

        [Theory]
        [MemberData(nameof(JobsWhichDontRequireJittingNames))]
        public void ForJobsThatDontRequireJittingOnlyGlobalSetupIsCalled(string jobName)
        {
            var job = JobsWhichDontRequireJitting[jobName];
            var engineParameters = CreateEngineParameters(mainNoUnroll: Throwing, mainUnroll: Throwing, job: job);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunStages = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                Assert.True(stage is not EngineJitStage);
                didRunStages = true;
                break;
            }

            Assert.True(didRunStages);
            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesIterationSetupCalled);
            Assert.Equal(0, timesBenchmarkCalled);
            Assert.Equal(0, timesOverheadCalled);
            Assert.Equal(0, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void ForDefaultSettingsVeryTimeConsumingBenchmarksAreExecutedOncePerIterationWithoutOverheadDeduction()
        {
            var engineParameters = CreateEngineParameters(mainNoUnroll: VeryTimeConsumingSingle, mainUnroll: InstantUnroll, job: Job.Default);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                Assert.NotEqual(IterationMode.Overhead, stage.Mode);

                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = engine.RunIteration(iterationData);
                    stageMeasurements.Add(measurement);
                }

                if (stage is EngineActualStage { Mode: IterationMode.Workload } actualStage)
                {
                    Assert.Equal(1, actualStage.invokeCount);
                    Assert.Equal(1, actualStage.unrollFactor);
                    didRunActualStage = true;
                    break;
                }
            }

            Assert.True(didRunActualStage);
            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispose

            engine.Dispose(); // cleanup is called as part of dispose

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Theory]
        [InlineData(120)] // 120 ms as in the bug report
        [InlineData(250)] // 250 ms as configured in dotnet/performance repo
        [InlineData(EngineResolver.DefaultIterationTime)] // 500 ms - the default BDN setting
        public void BenchmarksThatRunLongerThanIterationTimeOnlyDuringFirstInvocationAreNotInvokedOncePerIteration(int iterationTime)
        {
            var engineParameters = CreateEngineParameters(
                mainNoUnroll: TimeConsumingOnlyForTheFirstCall,
                mainUnroll: InstantUnroll,
                job: Job.Default.WithIterationTime(TimeInterval.FromMilliseconds(iterationTime)));

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = engine.RunIteration(iterationData);
                    stageMeasurements.Add(measurement);
                }

                if (stage is EngineActualStage { Mode: IterationMode.Workload } actualStage)
                {
                    Assert.NotEqual(1, actualStage.invokeCount + actualStage.unrollFactor);
                    didRunActualStage = true;
                    break;
                }
            }

            Assert.True(didRunActualStage);
            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.True(timesBenchmarkCalled >= 16);
            Assert.True(timesOverheadCalled >= 16);
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispose

            engine.Dispose(); // cleanup is called as part of dispose

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void ForJobsWithExplicitUnrollFactorTheGlobalSetupIsCalledAndMultiActionCodeGetsJitted()
            => AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job.Default.WithUnrollFactor(16));

        [Fact]
        public void ForJobsThatDontRequirePilotTheGlobalSetupIsCalledAndMultiActionCodeGetsJitted()
            => AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job.Default.WithInvocationCount(100));

        [Fact]
        public void NonVeryTimeConsumingBenchmarksAreExecutedMoreThanOncePerIterationWithUnrollFactorForDefaultSettings()
            => AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job.Default);

        private void AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job job)
        {
            var engineParameters = CreateEngineParameters(mainNoUnroll: InstantUnroll, mainUnroll: InstantUnroll, job: job);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = engine.RunIteration(iterationData);
                    stageMeasurements.Add(measurement);
                }

                Assert.IsType<EngineFirstJitStage>(stage);
                var jitStage = (EngineFirstJitStage) stage;
                Assert.True(jitStage.didJitUnroll);
                break;
            }

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.True(timesBenchmarkCalled >= 16);
            Assert.True(timesOverheadCalled >= 16);
            Assert.Equal(0, timesGlobalCleanupCalled);

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void MediumTimeConsumingBenchmarksShouldStartPilotFrom2AndIncrementItWithEveryStep()
        {
            var unrollFactor = Job.Default.ResolveValue(RunMode.UnrollFactorCharacteristic, DefaultResolver);

            const int times = 5; // how many times we should invoke the benchmark per iteration

            var mediumTime = TimeSpan.FromMilliseconds(IterationTime.TotalMilliseconds / times);

            void MediumNoUnroll(long invocationCount)
            {
                for (int i = 0; i < invocationCount; i++)
                {
                    timesBenchmarkCalled++;

                    Thread.Sleep(mediumTime);
                }
            }

            void MediumUnroll(long _)
            {
                timesBenchmarkCalled += unrollFactor;

                for (int i = 0; i < unrollFactor; i++) // the real unroll factor obviously does not use loop ;)
                    Thread.Sleep(mediumTime);
            }

            var engineParameters = CreateEngineParameters(mainNoUnroll: MediumNoUnroll, mainUnroll: MediumUnroll, job: Job.Default);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunPilotStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = engine.RunIteration(iterationData);
                    stageMeasurements.Add(measurement);
                }

                if (stage is EnginePilotAfterJitStage pilotStage)
                {
                    Assert.Equal(1, pilotStage.unrollFactor);
                    // We start from two (we know that 1 is not enough, the default is 4 so we need to override it).
                    Assert.Equal(2, pilotStage.minInvokeCount);
                    Assert.True(pilotStage.needsFurtherPilot);
                    Assert.False(pilotStage.evaluateOverhead);

                    didRunPilotStage = true;
                    break;
                }
            }

            Assert.True(didRunPilotStage);
            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        private EngineParameters CreateEngineParameters(Action<long> mainNoUnroll, Action<long> mainUnroll, Job job)
            => new EngineParameters
            {
                Dummy1Action = () => { },
                Dummy2Action = () => { },
                Dummy3Action = () => { },
                GlobalSetupAction = GlobalSetup,
                GlobalCleanupAction = GlobalCleanup,
                Host = new NoAcknowledgementConsoleHost(),
                OverheadActionUnroll = OverheadUnroll,
                OverheadActionNoUnroll = OverheadNoUnroll,
                IterationCleanupAction = IterationCleanup,
                IterationSetupAction = IterationSetup,
                WorkloadActionUnroll = mainUnroll,
                WorkloadActionNoUnroll = mainNoUnroll,
                TargetJob = job
            };
    }
}