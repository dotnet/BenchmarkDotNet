using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.IntegrationTests.WasmBenchmarks;

// These benchmarks live in their own project so the WASM tests can build/AOT them without pulling the xunit test
// framework into the benchmark app (AOT-compiling the xunit runner assemblies fails).

public class WasmBenchmark
{
    [Benchmark]
    public void Check()
    {
        if (!RuntimeInformation.IsWasm)
        {
            throw new Exception("Incorrect runtime detection");
        }
    }
}
