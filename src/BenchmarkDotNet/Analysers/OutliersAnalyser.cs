using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Analysers
{
    public class OutliersAnalyser : AnalyserBase
    {
        public override string Id => "Outliers";
        public static readonly IAnalyser Default = new OutliersAnalyser();

        private OutliersAnalyser() { }

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
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

            var cultureInfo = summary.GetCultureInfo();
            if (allOutliers.Any())
                yield return CreateHint(GetMessage(actualOutliers, allOutliers, statistics.LowerOutliers, statistics.UpperOutliers, cultureInfo), report);
        }

        /// <summary>
        /// Returns a nice message which can be displayed in the summary.
        /// </summary>
        /// <param name="actualOutliers">Actual outliers which were removed from the statistics</param>
        /// <param name="allOutliers">All outliers which present in the distribution (lower and upper)</param>
        /// <param name="lowerOutliers">All lower outliers</param>
        /// <param name="upperOutliers">All upper outliers</param>
        /// <param name="cultureInfo">CultureInfo</param>
        /// <returns>The message</returns>
        [PublicAPI, NotNull, Pure]
        public static string GetMessage(double[] actualOutliers, double[] allOutliers, double[] lowerOutliers, double[] upperOutliers, CultureInfo cultureInfo)
        {
            if (allOutliers.Length == 0)
                return string.Empty;

            string Format(int n, string verb)
            {
                string words = n == 1 ? "outlier  was " : "outliers were";
                return $"{n} {words} {verb}";
            }

            var rangeMessages = new List<string> { GetRangeMessage(lowerOutliers, cultureInfo), GetRangeMessage(upperOutliers, cultureInfo) };
            rangeMessages.RemoveAll(string.IsNullOrEmpty);
            string rangeMessage = rangeMessages.Any()
                ? " (" + string.Join(", ", rangeMessages) + ")"
                : string.Empty;

            if (actualOutliers.Length == allOutliers.Length)
                return Format(actualOutliers.Length, "removed") + rangeMessage;
            if (actualOutliers.Length == 0)
                return Format(allOutliers.Length, "detected") + rangeMessage;
            return Format(actualOutliers.Length, "removed") + ", " + Format(allOutliers.Length, "detected") + rangeMessage;
        }

        [CanBeNull]
        private static string GetRangeMessage([NotNull] double[] values, CultureInfo cultureInfo)
        {
            string Format(double value) => TimeInterval.FromNanoseconds(value).ToString(cultureInfo, "N2");

            switch (values.Length) {
                case 0:
                    return null;
                case 1:
                    return Format(values.First());
                case 2:
                    return Format(values.Min()) + ", " + Format(values.Max());
                default:
                    return Format(values.Min()) + ".." + Format(values.Max());
            }
        }
    }
}