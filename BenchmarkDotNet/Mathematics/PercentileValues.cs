using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Mathematics
{
    public enum PercentileLevel : int
    {
        P0 = 0,
        P25 = 25,
        P50 = 50,
        P67 = 67,
        P80 = 80,
        P85 = 85,
        P90 = 90,
        P95 = 95,
        P100 = 100
    }

    public static class PercentileLevelExtensions
    {
        public static double ToRatio(this PercentileLevel level)
        {
            return level == PercentileLevel.P67 ?
                 2.0 / 3.0 :
                 (int)level / 100.0;
        }
    }

    public class PercentileValues
    {
        /// <summary>
        /// Calculates the Nth percentile from the set of values
        /// </summary>
        /// <remarks>
        /// The implementation is expected to be consitent with the one from Excel.
        /// It's a quite common to export bench output into .csv for further analysis 
        /// And it's a good idea to have same results from all tools being used.
        /// </remarks>
        /// <param name="values">Sequence of the values to be calculated</param>
        /// <param name="percentileRatio">Value in range 0.0..1.0</param>
        /// <returns>Percentile from the set of values</returns>
        // BASEDON: http://stackoverflow.com/a/8137526
        private static double Percentile(List<double> sortedValues, double percentileRatio)
        {
            if (sortedValues == null)
                throw new ArgumentNullException(nameof(sortedValues));
            if (percentileRatio < 0 || percentileRatio > 1)
            {
                throw new ArgumentOutOfRangeException(
                     nameof(percentileRatio), percentileRatio,
                     "The percentileRatio arg should be in range of 0.0 - 1.0.");
            }

            if (sortedValues.Count == 0)
                return 0;

            // DONTTOUCH: the following code was taken from http://stackoverflow.com/a/8137526 and it is proven
            // to work in the same way the excel's counterpart does.
            // So it's better to leave it as it is unless you do not want to reimplement it from scratch:)
            double realIndex = percentileRatio * (sortedValues.Count - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < sortedValues.Count)
                return sortedValues[index] * (1 - frac) + sortedValues[index + 1] * frac;
            else
                return sortedValues[index];
        }

        public double Percentile(double ratio) => Percentile(SortedValues, ratio);
        public double Percentile(PercentileLevel percentileLevel) => Percentile(SortedValues, percentileLevel.ToRatio());

        private List<double> SortedValues { get; }

        public double P0 { get; }
        public double P25 { get; }
        public double P50 { get; }
        public double P67 { get; }
        public double P80 { get; }
        public double P85 { get; }
        public double P90 { get; }
        public double P95 { get; }
        public double P100 { get; }

        internal PercentileValues(List<double> sortedValues)
        {
            SortedValues = sortedValues;
            // TODO: Collect all in one call?
            P0 = Percentile(PercentileLevel.P0);
            P25 = Percentile(PercentileLevel.P25);
            P50 = Percentile(PercentileLevel.P50);
            P67 = Percentile(PercentileLevel.P67);
            P80 = Percentile(PercentileLevel.P80);
            P85 = Percentile(PercentileLevel.P85);
            P90 = Percentile(PercentileLevel.P90);
            P95 = Percentile(PercentileLevel.P95);
            P100 = Percentile(PercentileLevel.P100);
        }

        public string ToStr(bool showLevel = true) => $"[.95: {P95.ToStr()}] (0: {P0.ToStr()}]; .5: {P50.ToStr()}; 1: {P100.ToStr()})";
        public string ToTimeStr(TimeUnit unit = null, bool showLevel = true) => $"[.95: {P95.ToTimeStr(unit)}] (0: {P0.ToTimeStr(unit)}]; .5: {P50.ToTimeStr(unit)}; 1: {P100.ToTimeStr(unit)})";
    }
}