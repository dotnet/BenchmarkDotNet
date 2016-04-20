using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class PropertyColumn : IColumn
    {
        public static readonly IColumn Type = new PropertyColumn("Type", benchmark => benchmark.Target.Type.Name);
        public static readonly IColumn Method = new PropertyColumn("Method", benchmark => benchmark.Target.MethodTitle, true);
        public static readonly IColumn Mode = new PropertyColumn("Mode", benchmark => benchmark.Job.Mode.ToString());
        public static readonly IColumn Platform = new PropertyColumn("Platform", benchmark => benchmark.Job.Platform.ToString());
        public static readonly IColumn Jit = new PropertyColumn("Jit", benchmark => benchmark.Job.Jit.ToString());
        public static readonly IColumn Framework = new PropertyColumn("Framework", benchmark => benchmark.Job.Framework.ToString());
        public static readonly IColumn Runtime = new PropertyColumn("Runtime", benchmark => benchmark.Job.Runtime.ToString());
        public static readonly IColumn LaunchCount = new PropertyColumn("LaunchCount", benchmark => benchmark.Job.LaunchCount.ToString());
        public static readonly IColumn WarmupCount = new PropertyColumn("WarmupCount", benchmark => benchmark.Job.WarmupCount.ToString());
        public static readonly IColumn TargetCount = new PropertyColumn("TargetCount", benchmark => benchmark.Job.TargetCount.ToString());
        public static readonly IColumn Affinity = new PropertyColumn("Affinity", benchmark => benchmark.Job.Affinity.ToString());

        private readonly Func<Benchmark, string> calc;
        public string ColumnName { get; }
        public string GetValue(Summary summary, Benchmark benchmark) => calc(benchmark);
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow { get; }
        public ColumnCategory Category => ColumnCategory.Job;

        private PropertyColumn(string columnName, Func<Benchmark, string> calc, bool alwaysShow = false)
        {
            this.calc = calc;
            AlwaysShow = alwaysShow;
            ColumnName = columnName;
        }

        public override string ToString() => ColumnName;
    }
}