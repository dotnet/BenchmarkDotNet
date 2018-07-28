using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    public class OutliersAnalyser : AnalyserBase
    {
        public override string Id => "Outliers";
        public static readonly IAnalyser Default = new OutliersAnalyser();

        private OutliersAnalyser() { }

        public override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var workloadActual = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).ToArray();
            if (workloadActual.IsEmpty())
                yield break;
            var result = report.AllMeasurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Result)).ToArray();
            var outlierMode = report.BenchmarkCase.Job.ResolveValue(AccuracyMode.OutlierModeCharacteristic, EngineResolver.Instance); // TODO: improve            
            var statistics = workloadActual.GetStatistics();
            var allOutliers = statistics.AllOutliers;
            var actualOutliers = statistics.GetActualOutliers(outlierMode);

            if (result.Length + actualOutliers.Length != workloadActual.Length)
            {
                // This should never happen
                yield return CreateHint(
                    "Something went wrong with outliers: " +
                    $"Size(WorkloadActual) = {workloadActual.Length}, " +
                    $"Size(WorkloadActual/Outliers) = {actualOutliers.Length}, " +
                    $"Size(Result) = {result.Length}), " +
                    $"OutlierMode = {outlierMode}",
                    report);
                yield break;
            }

            if (allOutliers.Any())
                yield return CreateHint(GetMessage(actualOutliers.Length, allOutliers.Length), report);
        }

        /// <summary>
        /// Returns a nice message which can be displayed in the summary.
        /// </summary>
        /// <param name="actualOutliers">Actual outliers which were removed from the statistics</param>
        /// <param name="allOutliers">All outliers which present in the distribution (lower and upper)</param>
        /// <returns>The message</returns>
        [PublicAPI, NotNull, Pure]
        public static string GetMessage(int actualOutliers, int allOutliers)
        {
            string Format(int n, string verb)
            {
                string words = n == 1 ? "outlier  was " : "outliers were";
                return $"{n} {words} {verb}";
            }

            if (allOutliers == 0)
                return string.Empty;
            if (actualOutliers == allOutliers)
                return Format(actualOutliers, "removed");
            if (actualOutliers == 0)
                return Format(allOutliers, "detected");
            return Format(actualOutliers, "removed") + ", " + Format(allOutliers, "detected");
        }
    }
}