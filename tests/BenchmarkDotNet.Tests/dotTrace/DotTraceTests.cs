using BenchmarkDotNet.Diagnostics.dotTrace;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Tests.dotTrace;

public class DotTraceTests
{
    [Fact]
    public void AllRuntimeMonikerAreKnown()
    {
        var diagnoser = new DotTraceDiagnoser();
        foreach (RuntimeMoniker moniker in Enum.GetValues<RuntimeMoniker>())
            diagnoser.IsSupported(moniker); // Just check that it doesn't throw exceptions
    }
}