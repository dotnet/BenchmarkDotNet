using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Analysers
{
    public class RuntimeErrorAnalyser : AnalyserBase
    {
        public override string Id => "RuntimeError";
        public static readonly IAnalyser Default = new RuntimeErrorAnalyser();

        private RuntimeErrorAnalyser()
        {
        }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var errors = report.ExecuteResults.SelectMany(r => r.Data)
                .Union(report.ExecuteResults.SelectMany(r => r.ExtraOutput))
                .Where(line => line.Contains(ValidationErrorReporter.ConsoleErrorPrefix))
                .Select(line => line.Substring(ValidationErrorReporter.ConsoleErrorPrefix.Length).Trim());

            foreach (string error in errors)
                yield return CreateError(error, report);
        }
    }
}