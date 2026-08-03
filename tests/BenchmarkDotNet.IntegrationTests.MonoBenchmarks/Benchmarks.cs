using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.IntegrationTests.MonoBenchmarks;

// These benchmarks live in their own project so the Mono tests can build them against net8.0
// (Mono nuget packages are no longer published for net9.0+). Keeping them out of the main
// IntegrationTests project means that project doesn't need a net8.0 target framework.

public class MonoBenchmark
{
    [Benchmark]
    public void Check()
    {
        if (Type.GetType("Mono.RuntimeStructs") == null)
        {
            throw new Exception("This is not Mono runtime");
        }

        if (RuntimeInformation.GetCurrentRuntime() != MonoRuntime.Mono80)
        {
            throw new Exception("Incorrect runtime detection");
        }
    }
}

// A copy of MemoryDiagnoserTests.AccurateAllocations so the Mono memory test can run it under net8.0.
public class AccurateAllocations
{
    [Benchmark] public byte[] EightBytesArray() => new byte[8];
    [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];
    [Benchmark] public Task<int> AllocateTask() => Task.FromResult(-12345);
}
