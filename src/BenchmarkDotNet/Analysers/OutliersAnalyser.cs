﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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
            var outlierMode = report.BenchmarkCase.Job.ResolveValue(AccuracyMode.OutlierModeCharacteristic, EngineResolver.Instance); // TODO: improve
            var statistics = workloadActual.GetStatistics();
            var allOutliers = statistics.AllOutliers;
            var actualOutliers = statistics.GetActualOutliers(outlierMode);

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
        [PublicAPI, Pure]
        public static string GetMessage(double[] actualOutliers, double[] allOutliers, double[] lowerOutliers, double[] upperOutliers, CultureInfo cultureInfo)
        {
            if (allOutliers.Length == 0)
                return string.Empty;

            string Format(int n, string verb)
            {
                string words = n == 1 ? "outlier  was " : "outliers were";
                return $"{n} {words} {verb}";
            }

            var rangeMessages = new List<string?> { GetRangeMessage(lowerOutliers), GetRangeMessage(upperOutliers) };
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

        private static string? GetRangeMessage(double[] values)
        {
            string Format(double value) => TimeInterval.FromNanoseconds(value).ToDefaultString("N2");

            return values.Length switch
            {
                0 => null,
                1 => Format(values.First()),
                2 => Format(values.Min()) + ", " + Format(values.Max()),
                _ => Format(values.Min()) + ".." + Format(values.Max())
            };
        }
    }
}