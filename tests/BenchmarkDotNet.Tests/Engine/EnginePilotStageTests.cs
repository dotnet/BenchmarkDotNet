using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EnginePilotStageTests
    {
        private const long MaxPossibleInvokeCount = EnginePilotStage.MaxInvokeCount;
        private readonly ITestOutputHelper output;

        public EnginePilotStageTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AutoTest_BigResolution() => AutoTest(
            TimeInterval.Millisecond.ToFrequency(),
            TimeInterval.Millisecond,
            0.01,
            200);

        [Fact]
        public void AutoTest_ImpossibleResolution() => AutoTest(
            TimeInterval.Second.ToFrequency(),
            TimeInterval.Millisecond,
            0,
            EnginePilotStage.MaxInvokeCount);

        [Fact]
        public void SpecificTest_Simple() => SpecificTest(
            TimeInterval.Millisecond * 100,
            TimeInterval.Millisecond,
            64,
            128);

        private void AutoTest(Frequency clockFrequency, TimeInterval operationTime, double maxStdErrRelative, long minInvokeCount)
        {
            var job = Job.Default.
                With(new MockClock(clockFrequency)).
                WithMaxStdErrRelative(maxStdErrRelative);
            var stage = CreateStage(job, data => data.InvokeCount * operationTime);
            long invokeCount = stage.Run();
            output.WriteLine($"InvokeCount = {invokeCount} (Min= {minInvokeCount}, Max = {MaxPossibleInvokeCount})");
            Assert.InRange(invokeCount, minInvokeCount, MaxPossibleInvokeCount);
        }

        private void SpecificTest(TimeInterval iterationTime, TimeInterval operationTime, long minInvokeCount, long maxInvokeCount)
        {
            var job = Job.Default.
                With(new MockClock(Frequency.MHz)).
                WithIterationTime(iterationTime);
            var stage = CreateStage(job, data => data.InvokeCount * operationTime);
            long invokeCount = stage.Run();
            output.WriteLine($"InvokeCount = {invokeCount} (Min= {minInvokeCount}, Max = {maxInvokeCount})");
            Assert.InRange(invokeCount, minInvokeCount, maxInvokeCount);

        }

        private EnginePilotStage CreateStage(Job job, Func<IterationData, TimeInterval> measure)
        {
            var engine = new MockEngine(output, job, measure);
            return new EnginePilotStage(engine);
        }
    }
}