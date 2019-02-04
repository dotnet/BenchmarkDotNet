using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
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
            [Scenario]
            public int AnInteger() => 0;

            [Scenario]
            public void Nothing() { }

            [Scenario]
            public Task ATask() => Task.CompletedTask;

            [Scenario]
            public Task<int> TaskOfInt() => Task.FromResult<int>(0);

            [Scenario]
            public ValueTask<int> ValueTaskTaskOfInt() => new ValueTask<int>(0);
        }
    }
}