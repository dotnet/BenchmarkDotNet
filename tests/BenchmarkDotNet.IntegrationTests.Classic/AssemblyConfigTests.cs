using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    [DryJob]
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