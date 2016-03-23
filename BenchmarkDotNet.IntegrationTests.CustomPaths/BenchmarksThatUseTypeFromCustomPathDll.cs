using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatUseTypeFromCustomPathDll
    {
        [Benchmark]
        public string Benchmark()
        {
            return MachineType.Native.ToString();
        }
    }
}