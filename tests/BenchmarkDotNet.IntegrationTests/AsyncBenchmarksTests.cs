using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AsyncBenchmarksTests : BenchmarkTestExecutor
    {
        public AsyncBenchmarksTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TaskReturningMethodsAreAwaited()
        {
            var summary = CanExecute<TaskDelayMethods>();

            foreach (var report in summary.Reports)
            {
                foreach (var measurement in report.AllMeasurements)
                {
                    Assert.True(measurement.Nanoseconds > TaskDelayMethods.NanosecondsDelay);
                }
            }
        }

        public class TaskDelayMethods
        {
            private const int MilisecondsDelay = 100;

            internal const double NanosecondsDelay = MilisecondsDelay * 1e+6;

            [Benchmark]
            public Task ReturningTask() => Task.Delay(MilisecondsDelay);

            [Benchmark]
            public async Task Awaiting() => await Task.Delay(MilisecondsDelay);

            [Benchmark]
            public Task<int> ReturningGenericTask() => ReturningTask().ContinueWith(_ => default(int));

            [Benchmark]
            public ValueTask<int> ReturningValueTask() => new ValueTask<int>(ReturningGenericTask());
        }
    }
}