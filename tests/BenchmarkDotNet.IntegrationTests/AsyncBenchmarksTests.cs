using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AsyncBenchmarksTests : BenchmarkTestExecutor
    {
        public AsyncBenchmarksTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TaskReturningMethodsAreAwaited()
        {
            var summary = CanExecute<TaskDelayMethods>();

            foreach (var report in summary.Reports)
            foreach (var measurement in report.AllMeasurements)
            {
                double actual = measurement.Nanoseconds;
                const double minExpected = TaskDelayMethods.NanosecondsDelay - TaskDelayMethods.MaxTaskDelayResolutionInNanoseconds;
                string name = report.BenchmarkCase.Descriptor.GetFilterName();
                Assert.True(actual > minExpected, $"{name} has not been awaited, took {actual}ns, while it should take more than {minExpected}ns");
            }
        }

        public class TaskDelayMethods
        {
            private const int MillisecondsDelay = 100;

            internal const double NanosecondsDelay = MillisecondsDelay * 1e+6;

            // The default frequency of the Windows System Timer is 64Hz, so the Task.Delay error is up to 15.625ms.
            internal const int MaxTaskDelayResolutionInNanoseconds = 1_000_000_000 / 64;

            [Benchmark]
            public Task ReturningTask() => Task.Delay(MillisecondsDelay);

            [Benchmark]
            public ValueTask ReturningValueTask() => new ValueTask(Task.Delay(MillisecondsDelay));

            [Benchmark]
            public async Task Awaiting() => await Task.Delay(MillisecondsDelay);

            [Benchmark]
            public Task<int> ReturningGenericTask() => ReturningTask().ContinueWith(_ => default(int));

            [Benchmark]
            public ValueTask<int> ReturningGenericValueTask() => new ValueTask<int>(ReturningGenericTask());
        }
    }
}