using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class RuntimeErrorAnalyser : AnalyserBase
    {
        public override string Id => "RuntimeError";
        public static readonly IAnalyser Default = new RuntimeErrorAnalyser();

        private RuntimeErrorAnalyser()
        {
        }

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            foreach (string error in report.ExecuteResults.SelectMany(r => r.Errors))
                yield return CreateError(error, report);
        }
    }
}