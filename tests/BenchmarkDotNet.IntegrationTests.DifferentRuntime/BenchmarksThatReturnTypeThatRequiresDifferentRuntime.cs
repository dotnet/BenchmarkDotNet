using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.DifferentRuntime
{
    public class BenchmarksThatReturnTypeThatRequiresDifferentRuntime
    {
        [Benchmark]
        public Vector<int> Benchmark()
        {
            return Vector<int>.Zero;
        }
    }
}