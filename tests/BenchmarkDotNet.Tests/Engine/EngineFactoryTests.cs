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
        int timesBenchmarkCalled = 0, timesGlobalSetupCalled = 0, timesGlobalCleanupCalled = 0, timesIterationSetupCalled = 0, timesIterationCleanupCalled = 0;

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

        [Fact]
        public void VeryTimeConsumingBenchmarksAreExecutedOncePerIterationForDefaultSettings()
        {
            var engineParameters = CreateEngineParameters(singleAction: VeryTimeConsumingSingle, multiAction: Throwing, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(1, timesIterationSetupCalled);
            Assert.Equal(1, timesBenchmarkCalled);
            Assert.Equal(1, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled); // cleanup is called as part of dispode

            Assert.Equal(1, engine.TargetJob.Run.InvocationCount); // call the benchmark once per iteration
            Assert.Equal(1, engine.TargetJob.Run.UnrollFactor); // no unroll factor

            engine.Dispose(); // cleanup is called as part of dispode

            Assert.Equal(1, timesGlobalCleanupCalled);
        }
        
        [Fact]
        public void ForJobsThatDontRequireJittingOnlyGlobalSetupIsCalled()
        {
            var engineParameters = CreateEngineParameters(singleAction: Throwing, multiAction: Throwing, job: Job.Dry);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(0, timesIterationSetupCalled);
            Assert.Equal(0, timesBenchmarkCalled);
            Assert.Equal(0, timesIterationCleanupCalled);
            Assert.Equal(0, timesGlobalCleanupCalled); 

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }
        
        [Fact]
        public void NonVeryTimeConsumingBenchmarksAreExecutedMoreThanOncePerIterationWithUnrollFactorForDefaultSettings()
        {
            var engineParameters = CreateEngineParameters(singleAction: InstantSingle, multiAction: Instant16, job: Job.Default);

            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal(2, timesIterationSetupCalled); // once for single and & once for 16
            Assert.Equal(1 + 16, timesBenchmarkCalled);
            Assert.Equal(2, timesIterationCleanupCalled); // once for single and & once for 16
            Assert.Equal(0, timesGlobalCleanupCalled);

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic));

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        private EngineParameters CreateEngineParameters(Action<long> singleAction, Action<long> multiAction, Job job)
            => new EngineParameters
            {
                Dummy1Action = () => { },
                Dummy2Action = () => { },
                Dummy3Action = () => { },
                GlobalSetupAction = GlobalSetup,
                GlobalCleanupAction = GlobalCleanup,
                Host = new ConsoleHost(TextWriter.Null, TextReader.Null),
                IdleMultiAction = _ => { },
                IdleSingleAction = _ => { },
                IterationCleanupAction = IterationCleanup,
                IterationSetupAction = IterationSetup,
                MainMultiAction = multiAction,
                MainSingleAction = singleAction,
                Resolver = EngineResolver.Instance,
                TargetJob = job
            };
    }
}