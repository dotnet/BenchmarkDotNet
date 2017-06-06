using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics.PerfView
{
    public class MethodTimeFractionColumn : IColumn
    {
        private readonly string displayName;
        private readonly IDictionary<Benchmark, float[]> measurements;
        private readonly int mIndex;

        public MethodTimeFractionColumn(string displayName, IDictionary<Benchmark, float[]> measurements, int mIndex)
        {
            this.displayName = displayName;
            this.measurements = measurements;
            this.mIndex = mIndex;
        }
        
        public string Id => nameof(MethodTimeFractionColumn) + "_" + displayName;

        public string ColumnName => $"{displayName}";

        public bool AlwaysShow => false;

        public ColumnCategory Category => ColumnCategory.Custom;

        public int PriorityInCategory => mIndex + 1200;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "% of inclusive CPU stacks spent in the method";

        public string GetValue(Summary summary, Benchmark benchmark) =>
            measurements.TryGetValue(benchmark, out var times) ?
            $"{times[mIndex] * 100f}%" :
            "-";

        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) =>
            GetValue(summary, benchmark);

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    }
}
