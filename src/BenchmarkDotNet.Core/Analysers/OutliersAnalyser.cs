using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class OutliersAnalyser : AnalyserBase
    {
        public override string Id => "Outliers";
        public static readonly IAnalyser Default = new OutliersAnalyser();

        private OutliersAnalyser()
        {
        }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var actual = report.AllMeasurements.Where(m => m.IterationMode == IterationMode.MainTarget).ToArray();
            if (actual.IsEmpty())
                yield break;
            var result = report.AllMeasurements.Where(m => m.IterationMode == IterationMode.Result).ToArray();
            var actualOutliers = actual.GetStatistics().Outliers;
            bool removeOutliers = report.Benchmark.Job.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, EngineResolver.Instance); // TODO: improve

            if (result.Length + (actualOutliers.Length * (removeOutliers ? 1 : 0)) != actual.Length)
            {
                // This should never happen
                yield return CreateHint(
                    string.Format(
                        "Something went wrong with outliers: Size(MainTarget) = {0}, Size(MainTarget/Outliers) = {1}, Size(Result) = {2}), RemoveOutliers = {3}",
                        actual.Length, actualOutliers.Length, result.Length, removeOutliers),
                    report);
                yield break;
            }

            if (actualOutliers.Any())
            {
                int n = actualOutliers.Length;
                string words = n == 1 ? "outlier  was " : "outliers were";
                string verb = removeOutliers ? "removed" : "detected";
                yield return CreateHint($"{n} {words} {verb}", report);
            }
        }
    }
}