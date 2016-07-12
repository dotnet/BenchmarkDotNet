using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    [DryConfig]
    public class AssemblyConfigBenchmarks
    {
        [Benchmark]
        public void Foo()
        {
        }
    }

    public class AssemblyConfigTests
    {
        [Fact]
        public void IsActivatedTest()
        {
            BenchmarkRunner.Run<AssemblyConfigBenchmarks>();
            Assert.Equal(true, AssemblyConfigAttribute.IsActivated);
        }
    }
}