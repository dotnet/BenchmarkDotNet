using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Mathematics
{
    /// <summary>
    /// the goal of this struct is to avoid any heap allocations, please keep it in mind
    /// </summary>
    internal struct MeasurementsStatistics
    {
        /// <summary>
        /// Standard error in nanoseconds.
        /// </summary>
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

        public static MeasurementsStatistics Calculate(List<Measurement> measurements, bool removeOutliers)
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

            if (!removeOutliers) // most simple scenario is done without allocations! but this is not the default case
                return new MeasurementsStatistics(standardError, mean, confidenceInterval);

            measurements.Sort(); // sort in place

            double q1, median, q3;

            if (n == 1)
                q1 = median = q3 = measurements[0].Nanoseconds;
            else
            {
                q1 = GetQuartile(measurements, measurements.Count / 2);
                median = GetQuartile(measurements, measurements.Count);
                q3 = GetQuartile(measurements, measurements.Count * 3 / 2);
            }

            double interquartileRange = q3 - q1;
            double lowerFence = q1 - 1.5 * interquartileRange;
            double upperFence = q3 + 1.5 * interquartileRange;

            SumWithoutOutliers(measurements, lowerFence, upperFence, out sum, out n); // updates sum and N
            mean = sum / n;

            variance = VarianceWithoutOutliers(measurements, n, mean, lowerFence, upperFence);
            standardDeviation = Math.Sqrt(variance);
            standardError = standardDeviation / Math.Sqrt(n);
            confidenceInterval = new ConfidenceInterval(mean, standardError, n);

            return new MeasurementsStatistics(standardError, mean, confidenceInterval);
        }

        private static double Sum(List<Measurement> measurements)
        {
            double sum = 0;
            for (int i = 0; i < measurements.Count; i++)
                sum += measurements[i].Nanoseconds;
            return sum;
        }

        private static void SumWithoutOutliers(List<Measurement> measurements,
            double lowerFence, double upperFence, out double sum, out int n)
        {
            sum = 0;
            n = 0;

            for (int i = 0; i < measurements.Count; i++)
                if (!IsOutlier(measurements[i].Nanoseconds, lowerFence, upperFence))
                {
                    sum += measurements[i].Nanoseconds;
                    ++n;
                }
        }

        private static double Variance(List<Measurement> measurements, int n, double mean)
        {
            if (n == 1)
                return 0;

            double variance = 0;
            for (int i = 0; i < measurements.Count; i++)
                variance += (measurements[i].Nanoseconds - mean) * (measurements[i].Nanoseconds - mean) / (n - 1);

            return variance;
        }

        private static double VarianceWithoutOutliers(List<Measurement> measurements, int n, double mean, double lowerFence, double upperFence)
        {
            if (n == 1)
                return 0;

            double variance = 0;
            for (int i = 0; i < measurements.Count; i++)
                if (!IsOutlier(measurements[i].Nanoseconds, lowerFence, upperFence))
                    variance += (measurements[i].Nanoseconds - mean) * (measurements[i].Nanoseconds - mean) / (n - 1);

            return variance;
        }

        private static double GetQuartile(List<Measurement> measurements, int count)
        {
            if (count % 2 == 0)
                return (measurements[count / 2 - 1].Nanoseconds + measurements[count / 2].Nanoseconds) / 2;

            return measurements[count / 2].Nanoseconds;
        }

        private static bool IsOutlier(double value, double lowerFence, double upperFence)
            => value < lowerFence || value > upperFence;
    }
}
