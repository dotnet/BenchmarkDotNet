using System;
using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.dotMemory
{
    public class DotMemoryTests
    {
        [Fact]
        public void AllRuntimeMonikerAreKnown()
        {
            foreach (RuntimeMoniker moniker in Enum.GetValues(typeof(RuntimeMoniker)))
                DotMemoryDiagnoser.IsSupported(moniker); // Just check that it doesn't throw exceptions
        }
    }
}