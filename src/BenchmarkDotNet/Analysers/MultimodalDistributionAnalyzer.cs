using System.Collections.Generic;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    public class MultimodalDistributionAnalyzer : AnalyserBase
    {
        public static readonly IAnalyser Default = new MultimodalDistributionAnalyzer();

        private MultimodalDistributionAnalyzer() { }

        public override string Id => "MultimodalDistribution";

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var statistics = report.ResultStatistics;
            if (statistics == null)
                yield break;
            if (statistics.N < EngineResolver.DefaultMinTargetIterationCount)
                yield return CreateHint($"The number of iterations was set to less than {EngineResolver.DefaultMinTargetIterationCount}, " +
                                        $"{nameof(MultimodalDistributionAnalyzer)} needs at least {EngineResolver.DefaultMinTargetIterationCount} iterations to work."); 
                    
            double mValue = MathHelper.CalculateMValue(statistics);
            if (mValue > 4.2)
                yield return Create("is multimodal", mValue, report);
            else if (mValue > 3.2)
                yield return Create("is bimodal", mValue, report);
            else if (mValue > 2.8)
                yield return Create("can have several modes", mValue, report);
        }

        [NotNull]
        private Conclusion Create([NotNull] string kind, double mValue, [CanBeNull] BenchmarkReport report)
            => CreateWarning($"It seems that the distribution {kind} (mValue = {mValue})", report);
    }
}