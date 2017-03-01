using BenchmarkDotNet.Attributes;
using VerySimple;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatUseTypeFromCustomPathDll
    {
        [Benchmark]
        public string Benchmark() => new SingleClass().ToString();
    }
}