using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;

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

            var columNames = string.Join(",", columns);
            foreach (var benchmarkCase in summary.BenchmarksCases.Where(c => ResultsAreInvalid(summary, c)))
            {
                string message = "A question mark '?' symbol indicates that it was not possible " +
                                $"to compute ({columNames}) because the baseline value is too close to zero.";
                yield return Conclusion.CreateWarning(Id, message);
            }
        }

        private static bool ResultsAreInvalid(Summary summary, BenchmarkCase benchmarkCase)
        {
            var logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var baseline = summary.GetBaseline(logicalGroupKey);

            return baseline == null ||
                   summary[baseline] == null ||
                   summary[baseline].ResultStatistics == null ||
                   !summary[baseline].ResultStatistics.CanBeInverted() ||
                   summary[benchmarkCase] == null ||
                   summary[benchmarkCase].ResultStatistics == null;
        }
    }
}
