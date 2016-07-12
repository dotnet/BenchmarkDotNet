using BenchmarkDotNet.Attributes;
using VerySimple;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatUseTypeFromCustomPathDll
    {
        [Benchmark]
        public string Benchmark()
        {
            return new SingleClass().ToString();
        }
    }
}