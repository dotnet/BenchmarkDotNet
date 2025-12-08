using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class ConflictingNamesTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    [Fact]
    public void BenchmarkMethodsCanUseTemplateNames() => CanExecute<WithNamesUsedByTemplate>();

    public class WithNamesUsedByTemplate
    {
        [Params(1)]
        public int OverheadActionUnroll { get; set; }

        [Benchmark]
        [Arguments(2)]
        public void System(int OverheadActionNoUnroll)
        {

        }

        [Benchmark]
        public void BenchmarkDotNet()
        {

        }

        [Benchmark]
        public void __Overhead()
        {

        }

        [Benchmark]
        [Arguments(3)]
        public void WorkloadActionUnroll(int WorkloadActionNoUnroll)
        {

        }
    }
}