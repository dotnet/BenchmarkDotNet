using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineTargetStageTests
    {
        private const int MinIterationCount = EngineTargetStage.MinIterationCount;
        private const int MaxIterationCount = EngineTargetStage.MaxIterationCount;
        private const int MaxIdleIterationCount = EngineTargetStage.MaxIdleIterationCount;

        private readonly ITestOutputHelper output;

        public EngineTargetStageTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AutoTest_SteadyState() => AutoTest(data => TimeInterval.Second, MinIterationCount);

        [Fact]
        public void AutoTest_InfiniteIncrease() => AutoTest(data => TimeInterval.Second * data.Index, MaxIterationCount);

        [Fact]
        public void AutoTest_InfiniteIncreaseIdle() => AutoTest(data => TimeInterval.Second * data.Index, MaxIdleIterationCount, mode: IterationMode.IdleTarget);

        private void AutoTest(Func<IterationData, TimeInterval> measure, int min, int max = -1, IterationMode mode = IterationMode.MainTarget)
        {
            if (max == -1)
                max = min;
            var job = Job.Default;
            var stage = CreateStage(job, measure);
            var measurements = stage.Run(1, mode, Characteristic<int>.Create(""));
            int count = measurements.Count;
            output.WriteLine($"MeasurementCount = {count} (Min= {min}, Max = {max})");
            Assert.InRange(count, min, max);
        }

        private EngineTargetStage CreateStage(Job job, Func<IterationData, TimeInterval> measure)
        {
            var engine = new MockEngine(output, job, measure);
            return new EngineTargetStage(engine);
        }
    }
}