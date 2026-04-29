using BenchmarkDotNet.IntegrationTests.ConfigPerAssembly;

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