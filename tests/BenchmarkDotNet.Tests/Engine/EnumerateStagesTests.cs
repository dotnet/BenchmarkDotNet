using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using JetBrains.Annotations;
using Perfolizer.Horology;

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
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
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
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
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
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
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
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
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

        [Theory]
        [InlineData(1)]  // #3102: RunOncePerIteration auto-applied for [IterationSetup]/[IterationCleanup]
        [InlineData(2)]  // small explicit count, JIT stage repeats it to cover TieredCallCountThreshold
        [InlineData(5)]
        [InlineData(30)] // matches the default TieredCallCountThreshold — one yield per tier
        [InlineData(50)] // exceeds the default TieredCallCountThreshold — one yield per tier
        public void JobWithExplicitInvocationCount(long invocationCount)
        {
            // When the user pins InvocationCount (e.g. via [IterationSetup]/[IterationCleanup] which
            // implies RunOncePerIteration), every stage — including the JIT stage — must honor it so
            // the user's IterationSetup/IterationCleanup runs around the requested number of calls.
            var job = Job.Default.WithInvocationCount(invocationCount).WithUnrollFactor(1);
            var engineParameters = CreateEngineParameters(job);

            // A short measurement encourages the JIT stage to batch many invocations into a single iteration,
            // which is the regression introduced by #2806.
            var fastMeasurement = TimeInterval.FromMicroseconds(1);
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    if (iterationData.mode == IterationMode.Workload)
                    {
                        Assert.Equal(invocationCount, iterationData.invokeCount);
                    }
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, iterationData.invokeCount, fastMeasurement.Nanoseconds);
                    stageMeasurements.Add(measurement);
                }
            }
        }

        [Fact]
        public void LongRunningBenchmarksExitJitStageEarly()
        {
            // #3114: a benchmark whose single invocation exceeds IterationTime shouldn't drag
            // the JIT stage through the full tier-promotion loop. The pre-loop iteration
            // absorbs cctors/lazy-init; a confirmation iteration trips the long-running
            // heuristic and bails.
            var slowMeasurement = TimeInterval.FromSeconds(4); // ~8x default IterationTime of 500ms
            var job = Job.Default.WithInvocationCount(1).WithUnrollFactor(1);
            var engineParameters = CreateEngineParameters(job);

            int jitWorkloadCount = 0;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    if (stage is EngineJitStage && iterationData.mode == IterationMode.Workload)
                    {
                        jitWorkloadCount++;
                    }
                    var measurement = new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, iterationData.invokeCount, slowMeasurement.Nanoseconds);
                    stageMeasurements.Add(measurement);
                }

                if (stage is EngineJitStage) break;
            }

            // Non-tiered runtimes yield exactly one iteration; tiered runtimes yield two
            // (the pre-loop one, then a single confirmation before bailing).
            Assert.Equal(JitInfo.IsTiered ? 2 : 1, jitWorkloadCount);
        }

        [Fact]
        public void SlowFirstIterationButFastSteadyStateDoesNotExitJitStageEarly()
        {
            // If only the first iteration looks long-running (e.g. expensive cctor / lazy init),
            // the confirmation iteration disagrees and the tiering loop continues as before.
            if (!JitInfo.IsTiered) return; // Tier-promotion loop is skipped entirely on non-tiered runtimes.

            var slowFirst = TimeInterval.FromSeconds(4).Nanoseconds;
            var fastRest = TimeInterval.FromMicroseconds(1).Nanoseconds;
            var engineParameters = CreateEngineParameters(Job.Default.WithInvocationCount(1).WithUnrollFactor(1));

            int jitWorkloadCount = 0;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    if (stage is EngineJitStage && iterationData.mode == IterationMode.Workload)
                    {
                        jitWorkloadCount++;
                    }
                    var ns = jitWorkloadCount == 1 && stage is EngineJitStage ? slowFirst : fastRest;
                    stageMeasurements.Add(new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, iterationData.invokeCount, ns));
                }

                if (stage is EngineJitStage) break;
            }

            // Pre-loop iter + confirmation + full tiering loop (one yield per tier since the user pinned
            // InvocationCount=1, matching JitInfo.MaxTierPromotions * TieredCallCountThreshold) + the trailing
            // stabilization iteration. Just assert it ran the full tiering loop rather than bailing.
            Assert.True(jitWorkloadCount > 2, $"Expected the tiering loop to run after confirmation disagreed, got {jitWorkloadCount} jitting iterations.");
        }

        [FactEnvSpecific("Requires tiered JIT", EnvRequirement.DotNetCoreOnly)]
        public void ForceJitTieringModeRunsFullTieringLoopEvenForLongRunningBenchmarks()
        {
            // Tier-promotion loop is skipped entirely on non-tiered runtimes, so there is nothing to force.
            if (!JitInfo.IsTiered) return;

            // A benchmark whose single invocation far exceeds IterationTime would normally trip the
            // long-running heuristic and bail (see LongRunningBenchmarksExitJitStageEarly).
            // JitTieringMode.Force opts out of that heuristic and always promotes through every tier.
            var slowMeasurement = TimeInterval.FromSeconds(4); // ~8x default IterationTime of 500ms
            var job = Job.Default.WithInvocationCount(1).WithUnrollFactor(1).WithJitTieringMode(JitTieringMode.Force);
            var engineParameters = CreateEngineParameters(job);

            int jitWorkloadCount = 0;
            bool didStopEarly = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    if (stage is EngineJitStage && iterationData.mode == IterationMode.Workload)
                    {
                        jitWorkloadCount++;
                    }
                    stageMeasurements.Add(new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, iterationData.invokeCount, slowMeasurement.Nanoseconds));
                }

                if (stage is EngineJitStage jitStage)
                {
                    didStopEarly = jitStage.didStopEarly;
                    break;
                }
            }

            Assert.False(didStopEarly, "Force mode should never bail out of the JIT stage early.");
            Assert.True(jitWorkloadCount > 2, $"Expected the full tiering loop to run under Force mode, got {jitWorkloadCount} jitting iterations.");
        }

        [Fact]
        public void SkipJitTieringModeSkipsTierPromotion()
        {
            // JitTieringMode.Skip runs only the initial workload iteration and leaves tier promotion to the
            // following Pilot/Warmup stages (as on non-tiered runtimes). Unlike the long-running bail-out,
            // it does not flag the benchmark as long-running, so the Pilot stage still runs normally.
            var fastMeasurement = TimeInterval.FromMicroseconds(1); // would normally run the full tiering loop
            var job = Job.Default.WithInvocationCount(1).WithUnrollFactor(1).WithJitTieringMode(JitTieringMode.Skip);
            var engineParameters = CreateEngineParameters(job);

            int jitWorkloadCount = 0;
            bool didStopEarly = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
            {
                var stageMeasurements = stage.GetMeasurementList();
                while (stage.GetShouldRunIteration(stageMeasurements, out var iterationData))
                {
                    if (stage is EngineJitStage && iterationData.mode == IterationMode.Workload)
                    {
                        jitWorkloadCount++;
                    }
                    stageMeasurements.Add(new Measurement(0, iterationData.mode, iterationData.stage, iterationData.index, iterationData.invokeCount, fastMeasurement.Nanoseconds));
                }

                if (stage is EngineJitStage jitStage)
                {
                    didStopEarly = jitStage.didStopEarly;
                    break;
                }
            }

            // Only the single pre-loop workload iteration runs; the tier-promotion loop is skipped.
            Assert.Equal(1, jitWorkloadCount);
            Assert.False(didStopEarly, "Skip mode should not flag the benchmark as long-running.");
        }

        [Fact]
        public void MediumTimeConsumingBenchmarksStartPilotFrom2AndIncrementItWithEveryStep()
        {
            const int times = 5; // how many times we should invoke the benchmark per iteration

            var mediumTime = TimeInterval.FromMilliseconds(IterationTime.TotalMilliseconds / times);

            var engineParameters = CreateEngineParameters(Job.Default);

            bool didRunPilotStage = false;
            foreach (var stage in EngineStage.EnumerateStages(engineParameters, skipJitDelays: true))
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
            Func<long, IClock, ValueTask<ClockSpan>> emptyAction = (_, _) => new(default(ClockSpan));
            return new()
            {
                WorkloadMethod = null,
                GlobalSetupAction = () => new(),
                GlobalCleanupAction = () => new(),
                Host = host,
                OverheadActionUnroll = emptyAction,
                OverheadActionNoUnroll = emptyAction,
                IterationCleanupAction = () => new(),
                IterationSetupAction = () => new(),
                WorkloadActionUnroll = emptyAction,
                WorkloadActionNoUnroll = emptyAction,
                TargetJob = job,
                BenchmarkName = "",
                InProcessDiagnoserHandler = new([], host, Diagnosers.RunMode.None, null!)
            };
        }
    }
}