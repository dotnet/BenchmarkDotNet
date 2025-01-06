using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.FileLocators
{
    public class AssemblyNameIsSetBenchmarks
    {
        [Benchmark]
        public string Benchmark()
        {
            return "This will only run when a FileLocator is set due to <AssemblyName> in the csproj";
        }
    }
}