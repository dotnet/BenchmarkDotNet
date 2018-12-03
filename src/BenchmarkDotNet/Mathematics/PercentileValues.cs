using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics
{
    public class PercentileValues
    {
        /// <summary>
        /// Calculates the Nth percentile from the set of values
        /// </summary>
        /// <remarks>
        /// The implementation is expected to be consistent with the one from Excel.
        /// It's a quite common to export bench output into .csv for further analysis 
        /// And it's a good idea to have same results from all tools being used.
        /// </remarks>
        /// <param name="sortedValues">Sequence of the values to be calculated</param>
        /// <param name="percentile">Value in range 0..100</param>
        /// <returns>Percentile from the set of values</returns>
        // Based on: http://stackoverflow.com/a/8137526
        private static double Percentile(IReadOnlyList<double> sortedValues, int percentile)
        {
            if (sortedValues == null)
                throw new ArgumentNullException(nameof(sortedValues));
            if (percentile < 0 || percentile > 100)
            {
                throw new ArgumentOutOfRangeException(
                     nameof(percentile), percentile,
                     "The percentile arg should be in range of 0 - 100.");
            }

            if (sortedValues.Count == 0)
                return 0;

            // DONTTOUCH: the following code was taken from http://stackoverflow.com/a/8137526 and it is proven
            // to work in the same way the excel's counterpart does.
            // So it's better to leave it as it is unless you do not want to reimplement it from scratch:)
            double realIndex = percentile / 100.0 * (sortedValues.Count - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < sortedValues.Count)
                return sortedValues[index] * (1 - frac) + sortedValues[index + 1] * frac;
            return sortedValues[index];
        }

        [PublicAPI] public double Percentile(int percentile) => Percentile(SortedValues, percentile);

        private IReadOnlyList<double> SortedValues { get; }

        public double P0 { get; }
        public double P25 { get; }
        public double P50 { get; }
        public double P67 { get; }
        public double P80 { get; }
        public double P85 { get; }
        public double P90 { get; }
        public double P95 { get; }
        public double P100 { get; }

        internal PercentileValues(IReadOnlyList<double> sortedValues)
        {
            SortedValues = sortedValues;

            // TODO: Collect all in one call?
            P0 = Percentile(0);
            P25 = Percentile(25);
            P50 = Percentile(50);
            P67 = Percentile(67);
            P80 = Percentile(80);
            P85 = Percentile(85);
            P90 = Percentile(90);
            P95 = Percentile(95);
            P100 = Percentile(100);
        }

        public string ToStr() => $"[P95: {P95.ToStr()}] [P0: {P0.ToStr()}]; [P50: {P50.ToStr()}]; [P100: {P100.ToStr()}]";
        [PublicAPI] public string ToTimeStr(TimeUnit unit = null, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            return $"[P95: {P95.ToTimeStr(unit, encoding)}] [P0: {P0.ToTimeStr(unit, encoding)}]; [P50: {P50.ToTimeStr(unit, encoding)}]; [P100: {P100.ToTimeStr(unit, encoding)})]";
        }
    }
}