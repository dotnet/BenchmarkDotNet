using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public class RankColumn : IColumn
    {
        private readonly NumeralSystem numeralSystem;

        public RankColumn(NumeralSystem system) => numeralSystem = system;

        [PublicAPI] public static readonly IColumn Arabic = new RankColumn(NumeralSystem.Arabic);
        [PublicAPI] public static readonly IColumn Roman = new RankColumn(NumeralSystem.Roman);
        [PublicAPI] public static readonly IColumn Stars = new RankColumn(NumeralSystem.Stars);

        public string Id => nameof(RankColumn) + "." + numeralSystem;
        public string ColumnName => Column.Rank;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var logicalGroup = summary
                .GetLogicalGroupForBenchmark(benchmarkCase)
                .Where(b => summary[b].ResultStatistics != null)
                .ToArray();
            int index = Array.IndexOf(logicalGroup, benchmarkCase);
            if (index == -1)
                return MetricColumn.UnknownRepresentation;

            var ranks = RankHelper.GetRanks(logicalGroup.Select(b => summary[b].ResultStatistics).ToArray());
            int rank = ranks[index];
            return numeralSystem.ToPresentation(rank);
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public int PriorityInCategory => (int) numeralSystem;
        public override string ToString() => ColumnName;
        public string Legend => $"Relative position of current benchmark mean among all benchmarks ({numeralSystem} style)";
    }
}