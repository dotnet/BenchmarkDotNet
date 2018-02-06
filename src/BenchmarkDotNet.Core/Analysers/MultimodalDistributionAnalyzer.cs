using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    public class MultimodalDistributionAnalyzer : AnalyserBase
    {
        public override string Id => "MultimodalDistribution";
        public static readonly IAnalyser Default = new MultimodalDistributionAnalyzer();

        private MultimodalDistributionAnalyzer() { }

        [NotNull]
        private Conclusion Create([NotNull] string kind, double mValue, [CanBeNull] BenchmarkReport report)
            => CreateWarning($"It seems that the distribution {kind} (mValue = {mValue})", report);

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var statistics = report.ResultStatistics;
            if (statistics == null || statistics.N < 15)
                yield break;
            double mValue = MathHelper.CalculateMValue(statistics);
            if (mValue > 4.2)
                yield return Create("is multimodal", mValue, report);
            else if (mValue > 3.2)
                yield return Create("is bimodal", mValue, report);
            else if (mValue > 2.8)
                yield return Create("can have several modes", mValue, report);
        }
    }
}