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
    public class EnumerateStagesTests
    {
        private static TimeSpan IterationTime => TimeSpan.FromMilliseconds(EngineResolver.Instance.Resolve(Job.Default, RunMode.IterationTimeCharacteristic).ToMilliseconds());
        private static TimeInterval IterationTimeInternal => TimeInterval.FromMilliseconds(IterationTime.Milliseconds);

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
        public void JobsThatDontRequireJittingSkipJitStage(string jobName)
        {
            var job = JobsWhichDontRequireJitting[jobName];
            var engineParameters = CreateEngineParameters(job);

            bool didRunStages = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters))
            {
                Assert.True(stage is not EngineJitStage);
                didRunStages = true;
                break;
            }

            Assert.True(didRunStages);
        }

        [Fact]
        public void DefaultSettingsVeryTimeConsumingBenchmarksAreExecutedOncePerIterationWithoutOverheadDeduction()
        {
            var engineParameters = CreateEngineParameters(Job.Default);

            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters))
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
        }

        [Theory]
        [InlineData(120, 120)] // 120 ms as in the bug report
        [InlineData(250, 250)] // 250 ms as configured in dotnet/performance repo
        [InlineData(EngineResolver.DefaultIterationTime, EngineResolver.DefaultIterationTime)] // 500 ms - the default BDN setting
        [InlineData(EngineResolver.DefaultIterationTime, 20000)] // 20 seconds - twice the cutoff threshold of the old jit stage heuristic #2004
        public void BenchmarksThatRunLongerThanIterationTimeOnlyDuringFirstInvocationAreInvokedMoreThanOncePerIteration(int iterationTime, int callTime)
        {
            var callTimeInterval = TimeInterval.FromMilliseconds(callTime);
            var engineParameters = CreateEngineParameters(Job.Default.WithIterationTime(TimeInterval.FromMilliseconds(iterationTime)));

            bool didRunActualStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, callTimeInterval.Nanoseconds);
                    stageMeasurements.Add(measurement);
                    callTimeInterval = TimeInterval.FromNanoseconds(1);
                }

                if (stage is EngineActualStage { Mode: IterationMode.Workload } actualStage)
                {
                    Assert.True(actualStage.invokeCount > 1);
                    didRunActualStage = true;
                    break;
                }
            }

            Assert.True(didRunActualStage);
        }

        [Fact]
        public void JobWithExplicitUnrollFactorUnrolls()
            => AssertUnroll(Job.Default.WithUnrollFactor(16));

        [Fact]
        public void JobWithExplicitInvocationCountUnrolls()
            => AssertUnroll(Job.Default.WithInvocationCount(100));

        [Fact]
        public void DefaultJobUnrolls()
            => AssertUnroll(Job.Default);

        private void AssertUnroll(Job job)
        {
            var engineParameters = CreateEngineParameters(job);

            bool didRunUnroll = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    didRunUnroll |= iterationData.unrollFactor > 1;
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, 1, 1);
                    stageMeasurements.Add(measurement);
                }
            }

            Assert.True(didRunUnroll);
        }

        [Fact]
        public void MediumTimeConsumingBenchmarksStartPilotFrom2AndIncrementItWithEveryStep()
        {
            const int times = 5; // how many times we should invoke the benchmark per iteration

            var mediumTime = TimeInterval.FromMilliseconds(IterationTime.TotalMilliseconds / times);

            var engineParameters = CreateEngineParameters(Job.Default);

            bool didRunPilotStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters))
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
        }

        private EngineParameters CreateEngineParameters(Job job)
        {
            var host = new NoAcknowledgementConsoleHost();
            return new()
            {
                GlobalSetupAction = () => { },
                GlobalCleanupAction = () => { },
                Host = host,
                OverheadActionUnroll = _ => { },
                OverheadActionNoUnroll = _ => { },
                IterationCleanupAction = () => { },
                IterationSetupAction = () => { },
                WorkloadActionUnroll = _ => { },
                WorkloadActionNoUnroll = _ => { },
                TargetJob = job,
                BenchmarkName = "",
                InProcessDiagnoserHandler = new([], host, Diagnosers.RunMode.None, null!)
            };
        }
    }
}