using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatReturnTypeFromCustomPathDll
    {
        [Benchmark]
        public MachineType Benchmark()
        {
            return MachineType.Native;
        }
    }
}
