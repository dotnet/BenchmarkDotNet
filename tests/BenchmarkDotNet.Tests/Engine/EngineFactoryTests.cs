using System;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineFactoryTests
    {
        int timesBenchmarkCalled = 0, timesIdleCalled = 0;
        int timesGlobalSetupCalled = 0, timesGlobalCleanupCalled = 0, timesIterationSetupCalled = 0, timesIterationCleanupCalled = 0;

        void GlobalSetup() => timesGlobalSetupCalled++;
        void IterationSetup() => timesIterationSetupCalled++;
        void IterationCleanup() => timesIterationCleanupCalled++;
        void GlobalCleanup() => timesGlobalCleanupCalled++;
        
        void Throwing(long _) => throw new InvalidOperationException("must NOT be called");
        
        void VeryTimeConsumingSingle(long _)
        {
            timesBenchmarkCalled++;
            Thread.Sleep(TimeSpan.FromMilliseconds(EngineResolver.Instance.Resolve(Job.Default, RunMode.IterationTimeCharacteristic).ToMilliseconds()));
        }
        
        void InstantSingle(long _) => timesBenchmarkCalled++;
        void Instant16(long _) => timesBenchmarkCalled += 16;
        
        void IdleSingle(long _) => timesIdleCalled++;
        void Idle16(long _) => timesIdleCalled += 16;

        [Fact]
        public void VeryTimeConsumingBenchmarksAreExecutedOncePerIterationForDefaultSettings()
        {
            var engineParameters = CreateEngineParameters(mainSingleAction: VeryTimeConsumingSingle, mainMultiAction: Throwing, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(1 + 1, timesIterationSetupCalled); // 1x for Idle, 1x for Target
            Assert.Equal(1, timesBenchmarkCalled);
            Assert.Equal(1, timesIdleCalled);
            Assert.Equal(1 + 1, timesIterationCleanupCalled); // 1x for Idle, 1x for Target
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispode

            Assert.Equal(1, engine.TargetJob.Run.InvocationCount); // call the benchmark once per iteration
            Assert.Equal(1, engine.TargetJob.Run.UnrollFactor); // no unroll factor

            engine.Dispose(); // cleanup is called as part of dispode

            Assert.Equal(1, timesGlobalCleanupCalled);
        }
        
        [Fact]
        public void ForJobsThatDontRequireJittingOnlyGlobalSetupIsCalled()
        {
            var engineParameters = CreateEngineParameters(mainSingleAction: Throwing, mainMultiAction: Throwing, job: Job.Dry);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesIterationSetupCalled);
            Assert.Equal(0, timesBenchmarkCalled);
            Assert.Equal(0, timesIdleCalled);
            Assert.Equal(0, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled); 

            engine.Dispose();

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
            var engineParameters = CreateEngineParameters(mainSingleAction: Throwing, mainMultiAction: Instant16, job: job);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(2, timesIterationSetupCalled);
            Assert.Equal(16, timesBenchmarkCalled);
            Assert.Equal(16, timesIdleCalled);
            Assert.Equal(2, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled); 

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }
        
        [Fact]
        public void NonVeryTimeConsumingBenchmarksAreExecutedMoreThanOncePerIterationWithUnrollFactorForDefaultSettings()
        {
            var engineParameters = CreateEngineParameters(mainSingleAction: InstantSingle, mainMultiAction: Instant16, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal((1+1) * (1+1), timesIterationSetupCalled); // (once for single and & once for 16) x (1x for Idle + 1x for Target)
            Assert.Equal(1 + 16, timesBenchmarkCalled);
            Assert.Equal(1 + 16, timesIdleCalled);
            Assert.Equal((1+1) * (1+1), timesIterationCleanupCalled); // (once for single and & once for 16) x (1x for Idle + 1x for Target)
            Assert.Equal(0, timesGlobalCleanupCalled);

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic));

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        private EngineParameters CreateEngineParameters(Action<long> mainSingleAction, Action<long> mainMultiAction, Job job)
            => new EngineParameters
            {
                Dummy1Action = () => { },
                Dummy2Action = () => { },
                Dummy3Action = () => { },
                GlobalSetupAction = GlobalSetup,
                GlobalCleanupAction = GlobalCleanup,
                Host = new ConsoleHost(TextWriter.Null, TextReader.Null),
                IdleMultiAction = Idle16,
                IdleSingleAction = IdleSingle,
                IterationCleanupAction = IterationCleanup,
                IterationSetupAction = IterationSetup,
                MainMultiAction = mainMultiAction,
                MainSingleAction = mainSingleAction,
                TargetJob = job
            };
    }
}