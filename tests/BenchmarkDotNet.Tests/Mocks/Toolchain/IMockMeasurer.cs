using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Mocks.Toolchain
{
    public interface IMockMeasurer
    {
        List<Measurement> Measure(BenchmarkCase benchmarkCase);
    }
}