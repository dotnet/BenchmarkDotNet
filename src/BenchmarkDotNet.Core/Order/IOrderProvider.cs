using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Order
{
    public interface IOrderProvider
    {
        IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks);
        IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary);
        string GetGroupKey(Benchmark benchmark, Summary summary);
    }
}