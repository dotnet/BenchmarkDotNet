using BenchmarkDotNet.IntegrationTests.ConfigPerAssembly;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AssemblyConfigTests : BenchmarkTestExecutor
    {
        public AssemblyConfigTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ConfigCanBeSetPerAssembly()
        {
            CanExecute<AssemblyConfigBenchmarks>();

            Assert.True(AssemblyConfigAttribute.IsActivated);
        }
    }
}