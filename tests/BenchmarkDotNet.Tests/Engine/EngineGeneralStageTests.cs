using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Mocks;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineActualStageTests
    {
        private const int MinIterationCount = EngineResolver.DefaultMinWorkloadIterationCount;
        private const int MaxIterationCount = EngineResolver.DefaultMaxWorkloadIterationCount;
        private const int MaxOverheadIterationCount = EngineActualStage.MaxOverheadIterationCount;

        private readonly ITestOutputHelper output;

        public EngineActualStageTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AutoTest_SteadyState() => AutoTest(data => TimeInterval.Second, MinIterationCount);

        [Fact]
        public void AutoTest_InfiniteIncrease() => AutoTest(data => TimeInterval.Second * data.Index, MaxIterationCount);

        [Fact]
        public void AutoTest_InfiniteIncreaseOverhead() => AutoTest(data => TimeInterval.Second * data.Index, MaxOverheadIterationCount,
            iterationMode: IterationMode.Overhead);

        private void AutoTest(Func<IterationData, TimeInterval> measure, int min, int max = -1, IterationMode iterationMode = IterationMode.Workload)
        {
            if (max == -1)
                max = min;
            var job = Job.Default;
            var stage = CreateStage(job, measure);
            var measurements = stage.Run(1, iterationMode, true, 1);
            int count = measurements.Count;
            output.WriteLine($"MeasurementCount = {count} (Min= {min}, Max = {max})");
            Assert.InRange(count, min, max);
        }

        private EngineActualStage CreateStage(Job job, Func<IterationData, TimeInterval> measure)
        {
            var engine = new MockEngine(output, job, measure);
            return new EngineActualStage(engine);
        }
    }
}