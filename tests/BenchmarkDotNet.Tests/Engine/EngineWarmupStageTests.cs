using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineWarmupStageTests
    {
        private const int MinIterationCount = EngineResolver.DefaultMinWarmupIterationCount;
        private const int MaxIterationCount = EngineResolver.DefaultMaxWarmupIterationCount;
        private const int MaxOverheadIterationCount = DefaultStoppingCriteriaFactory.MaxOverheadIterationCount;

        private readonly ITestOutputHelper output;

        public EngineWarmupStageTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AutoTest_SteadyState()
        {
            AutoTest(data => TimeInterval.Millisecond, MinIterationCount);
        }

        [Fact]
        public void AutoTest_InfiniteIncrease()
        {
            AutoTest(data => TimeInterval.Millisecond * data.Index, MaxIterationCount);
        }

        [Fact]
        public void AutoTest_Alternation()
        {
            AutoTest(data => TimeInterval.Millisecond * (data.Index % 2), MinIterationCount, MaxIterationCount);
        }

        [Fact]
        public void AutoTest_TenSteps()
        {
            AutoTest(data => TimeInterval.Millisecond * Math.Max(0, 10 - data.Index), 10, MaxIterationCount);
        }

        [Fact]
        public void AutoTest_WithoutSteadyStateOverhead()
        {
            AutoTest(data => TimeInterval.Millisecond * data.Index, MaxOverheadIterationCount, mode: IterationMode.Overhead);
        }

        [Fact]
        public void MinAndMaxWarmupCountAttributesCanForceAutoWarmup()
        {
            const int explicitWarmupCount = 1;

            var warmupCountEqualOne = DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(explicitWarmupCount));

            var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(WithForceAutoWarmup), warmupCountEqualOne);

            var mergedJob = benchmarkRunInfo.BenchmarksCases.Single().Job;

            Assert.Equal(EngineResolver.ForceAutoWarmup, mergedJob.Run.WarmupCount);
            Assert.Equal(2, mergedJob.Run.MinWarmupIterationCount);
            Assert.Equal(4, mergedJob.Run.MaxWarmupIterationCount);

            AutoTest(data => TimeInterval.Millisecond * (data.Index % 2), 2, 4, job: mergedJob);
        }

        [MinWarmupCount(2, forceAutoWarmup: true)]
        [MaxWarmupCount(4, forceAutoWarmup: true)]
        public class WithForceAutoWarmup
        {
            [Benchmark]
            public void Method() { }
        }

        private void AutoTest(Func<IterationData, TimeInterval> measure, int min, int max = -1, IterationMode mode = IterationMode.Workload, Job? job = null)
        {
            if (max == -1)
                max = min;
            var stage = CreateStage(job ?? Job.Default, measure);
            var measurements = stage.Run(1, mode, 1, RunStrategy.Throughput);
            int count = measurements.Count;
            output.WriteLine($"MeasurementCount = {count} (Min= {min}, Max = {max})");
            Assert.InRange(count, min, max);
        }

        private EngineWarmupStage CreateStage(Job job, Func<IterationData, TimeInterval> measure)
        {
            var engine = new MockEngine(output, job, measure);
            return new EngineWarmupStage(engine);
        }
    }
}