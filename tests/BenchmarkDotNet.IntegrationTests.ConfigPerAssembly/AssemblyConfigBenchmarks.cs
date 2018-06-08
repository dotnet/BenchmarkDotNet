using BenchmarkDotNet.Attributes;

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