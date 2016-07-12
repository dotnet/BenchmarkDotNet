using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.DifferentRuntime
{
    public class BenchmarksThatUseTypeThatRequiresDifferentRuntime
    {
        [Benchmark]
        public bool Benchmark()
        {
            return Vector<int>.Zero.Equals(Vector<int>.One);
        }
    }
}