using System;
using System.Reflection;
using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.dotMemory;

public class DotMemoryTests
{
    [Fact]
    public void AllRuntimeMonikerAreKnown()
    {
        var diagnoser = new DotMemoryDiagnoser();
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