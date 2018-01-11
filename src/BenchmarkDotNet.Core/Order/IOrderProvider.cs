using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Order
{
    public interface IOrderProvider
    {
        [NotNull]
        IEnumerable<Benchmark> GetExecutionOrder([NotNull] Benchmark[] benchmarks);

        [NotNull]
        IEnumerable<Benchmark> GetSummaryOrder([NotNull] Benchmark[] benchmarks, [NotNull] Summary summary);

        [CanBeNull]
        string GetHighlightGroupKey([NotNull] Benchmark benchmark);

        [CanBeNull]
        string GetLogicalGroupKey(IConfig config, [NotNull] Benchmark[] allBenchmarks, [NotNull] Benchmark benchmark);

        [NotNull]
        IEnumerable<string> GetLogicalGroupOrder([NotNull] IEnumerable<string> logicalGroups);
        
        bool SeparateLogicalGroups { get; }
    }
}