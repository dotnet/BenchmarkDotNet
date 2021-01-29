using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Mathematics
{
    /// <summary>
    /// the goal of this struct is to avoid any heap allocations, please keep it in mind
    /// </summary>
    internal readonly ref struct MeasurementsStatistics
    {
        /// <summary>
        /// Standard error in nanoseconds.
        /// </summary>
        [PublicAPI]
        public double StandardError { get; }

        /// <summary>
        /// Mean in nanoseconds.
        /// </summary>
        public double Mean { get; }

        /// <summary>
        /// 99.9% confidence interval in nanoseconds.
        /// </summary>
        public ConfidenceInterval ConfidenceInterval { get; }

        private MeasurementsStatistics(double standardError, double mean, ConfidenceInterval confidenceInterval)
        {
            StandardError = standardError;
            Mean = mean;
            ConfidenceInterval = confidenceInterval;
        }

        public static MeasurementsStatistics Calculate(List<Measurement> measurements, OutlierMode outlierMode)
        {
            int n = measurements.Count;
            if (n == 0)
                throw new InvalidOperationException("StatSummary: Sequence contains no elements");

            double sum = Sum(measurements);
            double mean = sum / n;

            double variance = Variance(measurements, n, mean);
            double standardDeviation = Math.Sqrt(variance);
            double standardError = standardDeviation / Math.Sqrt(n);
            var confidenceInterval = new ConfidenceInterval(mean, standardError, n);

            if (outlierMode == OutlierMode.DontRemove) // most simple scenario is done without allocations! but this is not the default case
                return new MeasurementsStatistics(standardError, mean, confidenceInterval);

            measurements.Sort(); // sort in place

            double q1, q3;

            if (n == 1)
                q1 = q3 = measurements[0].Nanoseconds;
            else
            {
                q1 = GetQuartile(measurements, measurements.Count / 2);
                q3 = GetQuartile(measurements, measurements.Count * 3 / 2);
            }

            double interquartileRange = q3 - q1;
            double lowerFence = q1 - 1.5 * interquartileRange;
            double upperFence = q3 + 1.5 * interquartileRange;

            SumWithoutOutliers(outlierMode, measurements, lowerFence, upperFence, out sum, out n); // updates sum and N
            mean = sum / n;

            variance = VarianceWithoutOutliers(outlierMode, measurements, n, mean, lowerFence, upperFence);
            standardDeviation = Math.Sqrt(variance);
            standardError = standardDeviation / Math.Sqrt(n);
            confidenceInterval = new ConfidenceInterval(mean, standardError, n);

            return new MeasurementsStatistics(standardError, mean, confidenceInterval);
        }

        private static double Sum(List<Measurement> measurements)
        {
            double sum = 0;
            foreach (var m in measurements)
                sum += m.Nanoseconds;
            return sum;
        }

        private static void SumWithoutOutliers(OutlierMode outlierMode, List<Measurement> measurements,
            double lowerFence, double upperFence, out double sum, out int n)
        {
            sum = 0;
            n = 0;

            foreach (var m in measurements)
                if (!IsOutlier(outlierMode, m.Nanoseconds, lowerFence, upperFence))
                {
                    sum += m.Nanoseconds;
                    ++n;
                }
        }

        private static double Variance(List<Measurement> measurements, int n, double mean)
        {
            if (n == 1)
                return 0;

            double variance = 0;
            foreach (var m in measurements)
                variance += (m.Nanoseconds - mean) * (m.Nanoseconds - mean) / (n - 1);

            return variance;
        }

        private static double VarianceWithoutOutliers(OutlierMode outlierMode, List<Measurement> measurements, int n, double mean, double lowerFence, double upperFence)
        {
            if (n == 1)
                return 0;

            double variance = 0;
            foreach (var m in measurements)
                if (!IsOutlier(outlierMode, m.Nanoseconds, lowerFence, upperFence))
                    variance += (m.Nanoseconds - mean) * (m.Nanoseconds - mean) / (n - 1);

            return variance;
        }

        private static double GetQuartile(List<Measurement> measurements, int count)
        {
            if (count % 2 == 0)
                return (measurements[count / 2 - 1].Nanoseconds + measurements[count / 2].Nanoseconds) / 2;

            return measurements[count / 2].Nanoseconds;
        }

        private static bool IsOutlier(OutlierMode outlierMode, double value, double lowerFence, double upperFence)
        {
            switch (outlierMode)
            {
                case OutlierMode.DontRemove:
                    return false;
                case OutlierMode.RemoveUpper:
                    return value > upperFence;
                case OutlierMode.RemoveLower:
                    return value < lowerFence;
                case OutlierMode.RemoveAll:
                    return value < lowerFence || value > upperFence;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outlierMode), outlierMode, null);
            }
        }
    }
}
