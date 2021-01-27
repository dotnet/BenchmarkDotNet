using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Multimodality;

namespace BenchmarkDotNet.Analysers
{
    public class MultimodalDistributionAnalyzer : AnalyserBase
    {
        public static readonly IAnalyser Default = new MultimodalDistributionAnalyzer();

        private MultimodalDistributionAnalyzer() { }

        public override string Id => "MultimodalDistribution";

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var statistics = report.ResultStatistics;
            if (statistics == null || statistics.N < EngineResolver.DefaultMinWorkloadIterationCount)
                yield break;

            double mValue = MValueCalculator.Calculate(statistics.OriginalValues);
            if (mValue > 4.2)
                yield return Create("is multimodal", mValue, report, summary.GetCultureInfo());
            else if (mValue > 3.2)
                yield return Create("is bimodal", mValue, report, summary.GetCultureInfo());
            else if (mValue > 2.8)
                yield return Create("can have several modes", mValue, report, summary.GetCultureInfo());
        }

        [NotNull]
        private Conclusion Create([NotNull] string kind, double mValue, [CanBeNull] BenchmarkReport report, CultureInfo cultureInfo)
            => CreateWarning($"It seems that the distribution {kind} (mValue = {mValue.ToString("0.##", cultureInfo)})", report);
    }
}