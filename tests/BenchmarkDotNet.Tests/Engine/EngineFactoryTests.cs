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

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

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
            var engineParameters = CreateEngineParameters(mainNoUnroll: VeryTimeConsumingSingle, mainUnroll: Throwing, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(2, timesIterationSetupCalled); // 2x for Target
            Assert.Equal(2, timesBenchmarkCalled);
            Assert.Equal(2, timesOverheadCalled);
            Assert.Equal(2, timesIterationCleanupCalled); // 2x for Target
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispose

            Assert.Equal(1, engine.TargetJob.Run.InvocationCount); // call the benchmark once per iteration
            Assert.Equal(1, engine.TargetJob.Run.UnrollFactor); // no unroll factor

            Assert.True(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // is set to false in explicit way
            Assert.False(engine.TargetJob.Accuracy.EvaluateOverhead); // don't evaluate overhead in that case

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

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            // the factory should call the benchmark:
            // 1st time with unroll factor to JIT the code
            // one more to check that the Jitting has not dominated the reported time
            // and one more time to JIT the 16 unroll factor case as it turned out that Jitting has dominated the time
            Assert.Equal(1 + 1 + 1, timesIterationSetupCalled);
            Assert.Equal(1 + 1 + 16, timesBenchmarkCalled);
            Assert.Equal(1 + 1 + 16, timesOverheadCalled);
            Assert.Equal(1 + 1 + 1, timesIterationCleanupCalled); // 2x for Target
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispose

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic)); // we need pilot stage

            Assert.False(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic));

            engine.Dispose(); // cleanup is called as part of dispose

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void ForJobsWithExplicitUnrollFactorTheGlobalSetupIsCalledAndMultiActionCodeGetsJitted()
            => AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job.Default.WithUnrollFactor(16));

        [Fact]
        public void ForJobsThatDontRequirePilotTheGlobalSetupIsCalledAndMultiActionCodeGetsJitted()
            => AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job.Default.WithInvocationCount(100));

        private void AssertGlobalSetupWasCalledAndMultiActionGotJitted(Job job)
        {
            var engineParameters = CreateEngineParameters(mainNoUnroll: Throwing, mainUnroll: InstantUnroll, job: job);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(1, timesIterationSetupCalled);
            Assert.Equal(16, timesBenchmarkCalled);
            Assert.Equal(16, timesOverheadCalled);
            Assert.Equal(1, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            Assert.False(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // remains untouched

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void NonVeryTimeConsumingBenchmarksAreExecutedMoreThanOncePerIterationWithUnrollFactorForDefaultSettings()
        {
            var engineParameters = CreateEngineParameters(mainNoUnroll: InstantNoUnroll, mainUnroll: InstantUnroll, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(1 + 1, timesIterationSetupCalled); // once for single and & once for 16
            Assert.Equal(1 + 16, timesBenchmarkCalled);
            Assert.Equal(1 + 16, timesOverheadCalled);
            Assert.Equal(1 + 1, timesIterationCleanupCalled); // once for single and & once for 16
            Assert.Equal(0, timesGlobalCleanupCalled);

            Assert.False(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // remains untouched

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic));

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

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(1, timesIterationSetupCalled);
            Assert.Equal(1, timesBenchmarkCalled); // we run it just once and we know how many times it should be invoked
            Assert.Equal(1, timesOverheadCalled);
            Assert.Equal(1, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled);

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic)); // we need to run the pilot!
            Assert.Equal(1, engine.TargetJob.Run.UnrollFactor); // no unroll factor!
            Assert.Equal(2,
                engine.TargetJob.Accuracy.MinInvokeCount); // we start from two (we know that 1 is not enough, the default is 4 so we need to override it)

            Assert.True(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // is set to false in explicit way
            Assert.False(engine.TargetJob.Accuracy.EvaluateOverhead); // don't evaluate overhead in that case

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