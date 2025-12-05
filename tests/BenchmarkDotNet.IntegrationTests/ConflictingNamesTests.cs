using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ConflictingNamesTests : BenchmarkTestExecutor
    {
        public ConflictingNamesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BenchmarkMethodsCanUseTemplateNames() => CanExecute<WithNamesUsedByTemplate>();

        public class WithNamesUsedByTemplate
        {
            [Benchmark]
            public void System()
            {

            }

            [Benchmark]
            public void BenchmarkDotNet()
            {

            }
        }
    }
}