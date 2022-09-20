using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class HideColumnsAnalyser : AnalyserBase
    {
        public static readonly IAnalyser Default = new HideColumnsAnalyser();

        public override string Id => nameof(HideColumnsAnalyser);

        protected override IEnumerable<Conclusion> AnalyseSummary(Summary summary)
        {
            var hiddenColumns = summary.GetTable(summary.Style).Columns.Where(c => c.WasHidden).ToArray();

            if (hiddenColumns.IsEmpty())
                yield break;

            var columnNames = string.Join(", ", hiddenColumns.Select(c => c.OriginalColumn.ColumnName));

            var message = $"Hidden columns: {columnNames}";
            yield return Conclusion.CreateHint(Id, message);
        }
    }
}