using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class RankColumn : IColumn
    {
        private readonly NumeralSystem system;

        public RankColumn(NumeralSystem system)
        {
            this.system = system;
        }

        public static readonly IColumn Arabic = new RankColumn(NumeralSystem.Arabic);
        public static readonly IColumn Roman = new RankColumn(NumeralSystem.Roman);
        public static readonly IColumn Stars = new RankColumn(NumeralSystem.Stars);

        public string Id => nameof(RankColumn) + "." + system;
        public string ColumnName => "Rank";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var ranks = RankHelper.GetRanks(summary.Reports.Select(r => r.ResultStatistics).ToArray());
            int index = Array.IndexOf(summary.Reports.Select(r => r.Benchmark).ToArray(), benchmark);
            int rank = ranks[index];
            return system.ToPresentation(rank);
        }

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public int PriorityInCategory => (int) system;
        public override string ToString() => ColumnName;
        public string Legend => $"Relative position of current benchmark mean among all benchmarks ({system} style)";
    }
}