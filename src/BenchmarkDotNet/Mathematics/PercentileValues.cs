using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using Perfolizer.Mathematics.QuantileEstimators;

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

            return SimpleQuantileEstimator.Instance.GetQuantileFromSorted(sortedValues, percentile / 100.0);
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

        public override string ToString() => ToString(DefaultCultureInfo.Instance);

        public string ToString(Func<double, string> formatter)
        {
            var builder = new StringBuilder();
            builder.Append("[P95: ");
            builder.Append(formatter(P95));
            builder.Append("]; [P0: ");
            builder.Append(formatter(P0));
            builder.Append("]; [P50: ");
            builder.Append(formatter(P50));
            builder.Append("]; [P100: ");
            builder.Append(formatter(P100));
            builder.Append("]");
            return builder.ToString();
        }

        public string ToString([CanBeNull] CultureInfo cultureInfo, string format = "0.##")
        {
            return ToString(x => x.ToString(format, cultureInfo));
        }
    }
}