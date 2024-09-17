using System;
using System.Reflection;
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
        {
            // Just check that it doesn't throw exceptions, ignoring deprecated values.
            if (typeof(RuntimeMoniker).GetMember(moniker.ToString())[0].GetCustomAttribute<ObsoleteAttribute>() == null)
            {
                diagnoser.IsSupported(moniker);
            }
        }
    }
}