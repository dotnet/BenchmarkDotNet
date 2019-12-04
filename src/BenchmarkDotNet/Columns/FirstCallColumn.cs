using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class FirstCallColumn : IColumn
    {
        public string Id => nameof(FirstCallColumn);

        public string ColumnName => "First Call";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Custom;

        public int PriorityInCategory => -1;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Time;

        public string Legend => "Excution time of the first call (Jitting included)";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase].Success || !summary[benchmarkCase].AllMeasurements.Any(IsFirstCall))
                return "-";

            var measurement = summary[benchmarkCase].AllMeasurements.Single(IsFirstCall);

            return measurement.Nanoseconds.ToTimeStr(summary.Style.TimeUnit, benchmarkCase.Config.Encoding);
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => summary.Reports.Any(report => report.AllMeasurements.Any(IsFirstCall));

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        private static bool IsFirstCall(Measurement m)
            => m.IterationStage == Engines.IterationStage.Jitting && m.IterationMode == Engines.IterationMode.Workload && m.Operations == 1;
    }
}
