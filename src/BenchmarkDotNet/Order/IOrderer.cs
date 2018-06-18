using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Order
{
    public interface IOrderer
    {
        [PublicAPI, NotNull]
        IEnumerable<BenchmarkCase> GetExecutionOrder([NotNull] BenchmarkCase[] benchmarksCase);

        [PublicAPI, NotNull]
        IEnumerable<BenchmarkCase> GetSummaryOrder([NotNull] BenchmarkCase[] benchmarksCase, [NotNull] Summary summary);

        [PublicAPI, CanBeNull]
        string GetHighlightGroupKey([NotNull] BenchmarkCase benchmarkCase);

        [PublicAPI, CanBeNull]
        string GetLogicalGroupKey(IConfig config, [NotNull] BenchmarkCase[] allBenchmarksCases, [NotNull] BenchmarkCase benchmarkCase);

        [PublicAPI, NotNull]
        IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups);
        
        [PublicAPI]
        bool SeparateLogicalGroups { get; }
    }
}