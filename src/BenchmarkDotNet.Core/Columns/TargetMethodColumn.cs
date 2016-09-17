using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class TargetMethodColumn : IColumn
    {
        public static readonly IColumn Namespace = new TargetMethodColumn("Namespace", benchmark => benchmark.Target.Type.Namespace);
        public static readonly IColumn Type = new TargetMethodColumn("Type", benchmark => benchmark.Target.Type.Name);
        public static readonly IColumn Method = new TargetMethodColumn("Method", benchmark => benchmark.Target.MethodDisplayInfo, true);

        private readonly Func<Benchmark, string> calc;
        public string ColumnName { get; }
        public string GetValue(Summary summary, Benchmark benchmark) => calc(benchmark);
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow { get; }
        public ColumnCategory Category => ColumnCategory.Job;

        private TargetMethodColumn(string columnName, Func<Benchmark, string> calc, bool alwaysShow = false)
        {
            this.calc = calc;
            AlwaysShow = alwaysShow;
            ColumnName = columnName;
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    }
}