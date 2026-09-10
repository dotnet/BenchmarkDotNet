using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System.Reflection;

namespace BenchmarkDotNet.Tests.dotMemory;

public class DotMemoryTests
{
    [Fact]
    public void AllRuntimeMonikersAreKnown()
    {
        var diagnoser = new DotMemoryDiagnoser();
        foreach (var field in typeof(RuntimeMoniker).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var moniker = (string)field.GetValue(null)!;
            diagnoser.IsSupported(Runtime.Parse(moniker)); // Just check that it doesn't throw exceptions
        }
    }
}
