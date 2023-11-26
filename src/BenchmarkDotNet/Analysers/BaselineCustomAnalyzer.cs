using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Analysers
{
    public class BaselineCustomAnalyzer : AnalyserBase
    {
        public static readonly IAnalyser Default = new BaselineCustomAnalyzer();

        public override string Id => nameof(BaselineCustomAnalyzer);

        protected override IEnumerable<Conclusion> AnalyseSummary(Summary summary)
        {
            var columns = summary.GetColumns().Where(t => t.GetType().IsSubclassOf(typeof(BaselineCustomColumn)))
                                              .Select(t => t.ColumnName)
                                              .Distinct()
                                              .ToArray();
            if (columns.IsEmpty())
                yield break;

            var columnNames = string.Join(", ", columns);

            foreach (var benchmarkCase in summary.BenchmarksCases)
            {
                string? logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
                var baseline = summary.GetBaseline(logicalGroupKey);
                if (BaselineCustomColumn.ResultsAreInvalid(summary, benchmarkCase, baseline) == false)
                    continue;

                var message = "A question mark '?' symbol indicates that it was not possible to compute the " +
                                $"({columnNames}) column(s) because the baseline value is too close to zero.";

                yield return Conclusion.CreateWarning(Id, message);
            }
        }
    }
}
