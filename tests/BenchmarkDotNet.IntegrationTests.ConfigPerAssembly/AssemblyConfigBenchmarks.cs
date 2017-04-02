using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.IntegrationTests.ConfigPerAssembly
{
    [DryJob]
    public class AssemblyConfigBenchmarks
    {
        [Benchmark]
        public void Foo()
        {
        }
    }
}