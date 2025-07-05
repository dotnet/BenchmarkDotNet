using System;
using System.Collections.Generic;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineFactoryTests
    {
        private int timesGlobalSetupCalled = 0, timesGlobalCleanupCalled = 0, timesIterationSetupCalled = 0, timesIterationCleanupCalled = 0;

        private static TimeSpan IterationTime => TimeSpan.FromMilliseconds(EngineResolver.Instance.Resolve(Job.Default, RunMode.IterationTimeCharacteristic).ToMilliseconds());
        private static TimeInterval IterationTimeInternal => TimeInterval.FromMilliseconds(IterationTime.Milliseconds);

        private void GlobalSetup() => timesGlobalSetupCalled++;
        private void IterationSetup() => timesIterationSetupCalled++;
        private void IterationCleanup() => timesIterationCleanupCalled++;
        private void GlobalCleanup() => timesGlobalCleanupCalled++;

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
            var engineParameters = CreateEngineParameters(job);

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
            Assert.Equal(0, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void ForDefaultSettingsVeryTimeConsumingBenchmarksAreExecutedOncePerIterationWithoutOverheadDeduction()
        {
            var engineParameters = CreateEngineParameters(Job.Default);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                Assert.NotEqual(IterationMode.Overhead, stage.Mode);

                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, IterationTimeInternal.Nanoseconds);
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
            var timeInterval = TimeInterval.FromMilliseconds(iterationTime);
            var engineParameters = CreateEngineParameters(Job.Default.WithIterationTime(timeInterval));

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, timeInterval.Nanoseconds);
                    stageMeasurements.Add(measurement);
                    timeInterval = TimeInterval.FromNanoseconds(1);
                }

                if (stage is EngineActualStage { Mode: IterationMode.Workload } actualStage)
                {
                    Assert.NotEqual(1, actualStage.invokeCount * actualStage.unrollFactor);
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
            var engineParameters = CreateEngineParameters(job);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, 1);
                    stageMeasurements.Add(measurement);
                }

                Assert.IsType<EngineFirstJitStage>(stage);
                var jitStage = (EngineFirstJitStage) stage;
                Assert.True(jitStage.didJitUnroll);
                break;
            }

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void MediumTimeConsumingBenchmarksShouldStartPilotFrom2AndIncrementItWithEveryStep()
        {
            const int times = 5; // how many times we should invoke the benchmark per iteration

            var mediumTime = TimeInterval.FromMilliseconds(IterationTime.TotalMilliseconds / times);

            var engineParameters = CreateEngineParameters(Job.Default);

            var engine = (Engines.Engine) new EngineFactory().CreateReadyToRun(engineParameters);
            bool didRunPilotStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engine.Parameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, mediumTime.Nanoseconds);
                    stageMeasurements.Add(measurement);
                }

                if (stage is EnginePilotStageInitial pilotStage)
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

        private EngineParameters CreateEngineParameters(Job job)
            => new()
            {
                Dummy1Action = () => { },
                Dummy2Action = () => { },
                Dummy3Action = () => { },
                GlobalSetupAction = GlobalSetup,
                GlobalCleanupAction = GlobalCleanup,
                Host = new NoAcknowledgementConsoleHost(),
                OverheadActionUnroll = _ => { },
                OverheadActionNoUnroll = _ => { },
                IterationCleanupAction = IterationCleanup,
                IterationSetupAction = IterationSetup,
                WorkloadActionUnroll = _ => { },
                WorkloadActionNoUnroll = _ => { },
                TargetJob = job
            };
    }
}