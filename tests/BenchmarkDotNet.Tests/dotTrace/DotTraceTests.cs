using System;
using BenchmarkDotNet.Diagnostics.dotTrace;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.dotTrace;

public class DotTraceTests
{
    [Fact]
    public void AllRuntimeMonikerAreKnown()
    {
        var diagnoser = new DotTraceDiagnoser();
        foreach (RuntimeMoniker moniker in Enum.GetValues(typeof(RuntimeMoniker)))
            diagnoser.IsSupported(moniker); // Just check that it doesn't throw exceptions
    }
}