using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineWarmupStageTests
    {
        private const int MinIterationCount = EngineWarmupStage.MinIterationCount;
        private const int MaxIterationCount = EngineWarmupStage.MaxIterationCount;
        private const int MaxIdleItertaionCount = EngineWarmupStage.MaxIdleItertaionCount;

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
        public void AutoTest_WithoutSteadyStateIdle()
        {
            AutoTest(data => TimeInterval.Millisecond * data.Index, MaxIdleItertaionCount, mode: IterationMode.IdleWarmup);
        }

        private void AutoTest(Func<IterationData, TimeInterval> measure, int min, int max = -1, IterationMode mode = IterationMode.MainWarmup)
        {
            if (max == -1)
                max = min;
            var job = Job.Default;
            var stage = CreateStage(job, measure);
            var measurements = stage.Run(1, mode, true, 1);
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