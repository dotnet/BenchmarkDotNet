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
            var engine = new MockEngine(output, job, measure);
            var stage = iterationMode == IterationMode.Overhead
                ? EngineActualStage.GetOverhead(engine)
                : EngineActualStage.GetWorkload(engine, RunStrategy.Throughput);
            var (_, measurements) = engine.Run(stage);
            int count = measurements.Count;
            output.WriteLine($"MeasurementCount = {count} (Min= {min}, Max = {max})");
            Assert.InRange(count, min, max);
        }
    }
}