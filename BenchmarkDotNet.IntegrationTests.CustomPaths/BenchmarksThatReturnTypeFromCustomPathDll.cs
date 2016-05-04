using BenchmarkDotNet.Attributes;
using VerySimple;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatReturnTypeFromCustomPathDll
    {
        [Benchmark]
        public SingleClass Benchmark()
        {
            return new SingleClass();
        }
    }
}
