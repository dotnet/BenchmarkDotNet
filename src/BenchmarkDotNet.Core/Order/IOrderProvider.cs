using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Order
{
    public interface IOrderProvider
    {
        [PublicAPI, NotNull]
        IEnumerable<Benchmark> GetExecutionOrder([NotNull] Benchmark[] benchmarks);

        [PublicAPI, NotNull]
        IEnumerable<Benchmark> GetSummaryOrder([NotNull] Benchmark[] benchmarks, [NotNull] Summary summary);

        [PublicAPI, CanBeNull]
        string GetHighlightGroupKey([NotNull] Benchmark benchmark);

        [PublicAPI, CanBeNull]
        string GetLogicalGroupKey(IConfig config, [NotNull] Benchmark[] allBenchmarks, [NotNull] Benchmark benchmark);

        [PublicAPI, NotNull]
        IEnumerable<IGrouping<string, Benchmark>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, Benchmark>> logicalGroups);
        
        [PublicAPI]
        bool SeparateLogicalGroups { get; }
    }
}