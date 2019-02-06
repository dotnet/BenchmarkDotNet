using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValuesReturnedByScenarioTest : BenchmarkTestExecutor
    {
        public ValuesReturnedByScenarioTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AnyValueCanBeReturned() => CanExecute<ValuesReturnedByScenario>();

        public class ValuesReturnedByScenario
        {
            [Benchmark(Kind = BenchmarkKind.Scenario)]
            public int AnInteger() => 0;

            [Benchmark(Kind = BenchmarkKind.Scenario)]
            public void Nothing() { }

            [Benchmark(Kind = BenchmarkKind.Scenario)]
            public Task ATask() => Task.CompletedTask;

            [Benchmark(Kind = BenchmarkKind.Scenario)]
            public Task<int> TaskOfInt() => Task.FromResult<int>(0);

            [Benchmark(Kind = BenchmarkKind.Scenario)]
            public ValueTask<int> ValueTaskTaskOfInt() => new ValueTask<int>(0);
        }
    }
}