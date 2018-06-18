using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class TargetMethodColumn : IColumn
    {
        public static readonly IColumn Namespace = new TargetMethodColumn("Namespace", benchmark => benchmark.Descriptor.Type.Namespace);
        public static readonly IColumn Type = new TargetMethodColumn("Type", benchmark => benchmark.Descriptor.Type.Name);
        public static readonly IColumn Method = new TargetMethodColumn("Method", benchmark => benchmark.Descriptor.MethodDisplayInfo, true);

        private readonly Func<BenchmarkCase, string> valueProvider;
        public string Id => nameof(TargetMethodColumn) + "." + ColumnName;
        public string ColumnName { get; }
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => valueProvider(benchmarkCase);
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow { get; }
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "";
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);

        private TargetMethodColumn(string columnName, Func<BenchmarkCase, string> valueProvider, bool alwaysShow = false)
        {
            this.valueProvider = valueProvider;
            AlwaysShow = alwaysShow;
            ColumnName = columnName;
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    }
}