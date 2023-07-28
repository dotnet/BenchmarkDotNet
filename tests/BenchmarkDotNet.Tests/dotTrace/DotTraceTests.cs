using System;
using BenchmarkDotNet.Diagnostics.dotTrace;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.dotTrace
{
    public class DotTraceTests
    {
        [Fact]
        public void AllRuntimeMonikerAreKnown()
        {
            foreach (RuntimeMoniker moniker in Enum.GetValues(typeof(RuntimeMoniker)))
                DotTraceDiagnoser.IsSupported(moniker); // Just check that it doesn't throw exceptions
        }
    }
}