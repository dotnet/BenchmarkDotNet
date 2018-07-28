using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    public abstract class AnalyserBase : IAnalyser
    {
        public abstract string Id { get; }

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            foreach (var conclusion in AnalyseSummary(summary))
                yield return conclusion;
            foreach (var report in summary.Reports)
                foreach (var conclusion in AnalyseReport(report, summary))
                    yield return conclusion;
        }

        protected virtual IEnumerable<Conclusion> AnalyseSummary(Summary summary) => Enumerable.Empty<Conclusion>();
        protected virtual IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary) => Enumerable.Empty<Conclusion>();

        protected Conclusion CreateHint(string message, [CanBeNull] BenchmarkReport report = null) => Conclusion.CreateHint(Id, message, report);
        protected Conclusion CreateWarning(string message, [CanBeNull] BenchmarkReport report = null) => Conclusion.CreateWarning(Id, message, report);
        protected Conclusion CreateError(string message, [CanBeNull] BenchmarkReport report = null) => Conclusion.CreateError(Id, message, report);
    }
}