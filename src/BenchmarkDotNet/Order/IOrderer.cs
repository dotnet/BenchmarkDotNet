using System.Collections.Generic;
using System.Collections.Immutable;
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
        IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase, IEnumerable<BenchmarkLogicalGroupRule> order = null);

        [PublicAPI, NotNull]
        IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, [NotNull] Summary summary);

        [PublicAPI, CanBeNull]
        string GetHighlightGroupKey([NotNull] BenchmarkCase benchmarkCase);

        [PublicAPI, CanBeNull]
        string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, [NotNull] BenchmarkCase benchmarkCase);

        [PublicAPI, NotNull]
        IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups,
            IEnumerable<BenchmarkLogicalGroupRule> order = null);

        [PublicAPI]
        bool SeparateLogicalGroups { get; }
    }
}