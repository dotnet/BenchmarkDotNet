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

        [PublicAPI] protected virtual IEnumerable<Conclusion> AnalyseSummary(Summary summary) => Enumerable.Empty<Conclusion>();
        [PublicAPI] protected virtual IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary) => Enumerable.Empty<Conclusion>();

        protected Conclusion CreateHint(string message, BenchmarkReport? report = null, bool mergeable = true)
            => Conclusion.CreateHint(Id, message, report, mergeable);
        protected Conclusion CreateWarning(string message, BenchmarkReport? report = null, bool mergeable = true)
            => Conclusion.CreateWarning(Id, message, report, mergeable);
        protected Conclusion CreateError(string message, BenchmarkReport? report = null, bool mergeable = true)
            => Conclusion.CreateError(Id, message, report, mergeable);
    }
}