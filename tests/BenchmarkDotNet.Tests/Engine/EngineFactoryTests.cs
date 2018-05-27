using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineFactoryTests
    {
        int timesBenchmarkCalled = 0, timesIdleCalled = 0;
        int timesGlobalSetupCalled = 0, timesGlobalCleanupCalled = 0, timesIterationSetupCalled = 0, timesIterationCleanupCalled = 0;
        
        TimeSpan IterationTime => TimeSpan.FromMilliseconds(EngineResolver.Instance.Resolve(Job.Default, RunMode.IterationTimeCharacteristic).ToMilliseconds());

        IResolver DefaultResolver => BenchmarkRunner.DefaultResolver;

        void GlobalSetup() => timesGlobalSetupCalled++;
        void IterationSetup() => timesIterationSetupCalled++;
        void IterationCleanup() => timesIterationCleanupCalled++;
        void GlobalCleanup() => timesGlobalCleanupCalled++;
        
        void Throwing(long _) => throw new InvalidOperationException("must NOT be called");
        
        void VeryTimeConsumingSingle(long _)
        {
            timesBenchmarkCalled++;
            Thread.Sleep(IterationTime);
        }
        
        void InstantSingle(long _) => timesBenchmarkCalled++;
        void Instant16(long _) => timesBenchmarkCalled += 16;
        
        void IdleSingle(long _) => timesIdleCalled++;
        void Idle16(long _) => timesIdleCalled += 16;

        public static IEnumerable<object[]> JobsWhichDontRequireJitting()
        {
            yield return new object[]{ Job.Dry };
            yield return new object[]{ Job.Default.With(RunStrategy.ColdStart) };
            yield return new object[]{ Job.Default.With(RunStrategy.Monitoring) };
        }

        [Theory]
        [MemberData(nameof(JobsWhichDontRequireJitting))]
        public void ForJobsThatDontRequireJittingOnlyGlobalSetupIsCalled(Job job)
        {
            var engineParameters = CreateEngineParameters(mainSingleAction: Throwing, mainMultiAction: Throwing, job: job);

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
        public void ForDefaultSettingsVeryTimeConsumingBenchmarksAreExecutedOncePerIterationWithoutOverheadDeduction()
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
            
            Assert.True(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // is set to false in explicit way
            Assert.False(engine.TargetJob.Accuracy.EvaluateOverhead); // don't evaluate overhead in that case

            engine.Dispose(); // cleanup is called as part of dispode

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
            
            Assert.False(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // remains untouched

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
            
            Assert.False(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // remains untouched

            Assert.False(engine.TargetJob.Run.HasValue(RunMode.InvocationCountCharacteristic));

            engine.Dispose();

            Assert.Equal(1, timesGlobalCleanupCalled);
        }

        [Fact]
        public void DontRunThePilotIfThePilotRequirementIsMetDuringWarmup()
        {
            var unrollFactor = Job.Default.ResolveValue(RunMode.UnrollFactorCharacteristic, DefaultResolver);
            var mediumTime =  TimeSpan.FromMilliseconds((IterationTime.TotalMilliseconds / unrollFactor) * 2);
            
            void MediumSingle(long _)
            {
                timesBenchmarkCalled++;

                Thread.Sleep(mediumTime);
            }
        
            void MediumMultiple(long _)
            {
                timesBenchmarkCalled += unrollFactor;
            
                for (int i = 0; i < unrollFactor; i++) // the real unroll factor obviously does not use loop ;)
                    Thread.Sleep(mediumTime);
            }
            
            var engineParameters = CreateEngineParameters(mainSingleAction: MediumSingle, mainMultiAction: MediumMultiple, job: Job.Default);
            
            var engine = new EngineFactory().CreateReadyToRun(engineParameters);

            Assert.Equal(1, timesGlobalSetupCalled);
            Assert.Equal((1+1) * (1+1), timesIterationSetupCalled); // (once for single and & once for 16) x (1x for Idle + 1x for Target)
            Assert.Equal(1 + unrollFactor, timesBenchmarkCalled);
            Assert.Equal(1 + unrollFactor, timesIdleCalled);
            Assert.Equal((1+1) * (1+1), timesIterationCleanupCalled); // (once for single and & once for 16) x (1x for Idle + 1x for Target)
            Assert.Equal(0, timesGlobalCleanupCalled);
            
            Assert.Equal(unrollFactor, engine.TargetJob.Run.InvocationCount); // no need to run pilot!
            Assert.Equal(unrollFactor, engine.TargetJob.Run.UnrollFactor);  // remains the same!
            
            Assert.True(engine.TargetJob.Run.HasValue(AccuracyMode.EvaluateOverheadCharacteristic)); // is set to false in explicit way
            Assert.False(engine.TargetJob.Accuracy.EvaluateOverhead); // don't evaluate overhead in that case

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