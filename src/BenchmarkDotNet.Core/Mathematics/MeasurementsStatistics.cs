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
        public double StandardError { get; }
        public double Mean { get; }

        private MeasurementsStatistics(double standardError, double mean)
        {
            StandardError = standardError;
            Mean = mean;
        }

        public static MeasurementsStatistics Calculate(List<Measurement> measurements, bool removeOutliers)
        {
            var N = measurements.Count;
            if (N == 0)
                throw new InvalidOperationException("StatSummary: Sequence contains no elements");

            var sum = Sum(measurements);
            var mean = sum / N;

            var variance = Variance(measurements, N, mean);
            var standardDeviation = Math.Sqrt(variance);
            var standardError = standardDeviation / Math.Sqrt(N);

            if (!removeOutliers) // most simple scenario is done without allocations! but this is not the default case
                return new MeasurementsStatistics(standardError, mean);

            measurements.Sort(); // sort in place

            double Q1, Median, Q3;

            if (N == 1)
                Q1 = Median = Q3 = measurements[0].Nanoseconds;
            else
            {
                Q1 = GetQuartile(measurements, measurements.Count / 2);
                Median = GetQuartile(measurements, measurements.Count);
                Q3 = GetQuartile(measurements, measurements.Count * 3 / 2);
            }

            var InterquartileRange = Q3 - Q1;
            var LowerFence = Q1 - 1.5 * InterquartileRange;
            var UpperFence = Q3 + 1.5 * InterquartileRange;

            SumWithoutOutliers(measurements, LowerFence, UpperFence, out sum, out N); // updates sum and N
            mean = sum / N;

            variance = VarianceWithoutOutliers(measurements, N, mean, LowerFence, UpperFence);
            standardDeviation = Math.Sqrt(variance);
            standardError = standardDeviation / Math.Sqrt(N);

            return new MeasurementsStatistics(standardError, mean);
        }

        private static double Sum(List<Measurement> measurements)
        {
            double sum = 0;
            for (int i = 0; i < measurements.Count; i++)
                sum += measurements[i].Nanoseconds;
            return sum;
        }

        private static void SumWithoutOutliers(List<Measurement> measurements,
            double lowerFence, double upperFence, out double sum, out int N)
        {
            sum = 0;
            N = 0;

            for (int i = 0; i < measurements.Count; i++)
                if (!IsOutlier(measurements[i].Nanoseconds, lowerFence, upperFence))
                {
                    sum += measurements[i].Nanoseconds;
                    ++N;
                }
        }

        private static double Variance(List<Measurement> measurements, int N, double mean)
        {
            if (N == 1)
                return 0;

            double variance = 0;
            for (int i = 0; i < measurements.Count; i++)
                variance += Math.Pow(measurements[i].Nanoseconds - mean, 2) / (N - 1);

            return variance;
        }

        private static double VarianceWithoutOutliers(List<Measurement> measurements, int N, double mean, double lowerFence, double upperFence)
        {
            if (N == 1)
                return 0;

            double variance = 0;
            for (int i = 0; i < measurements.Count; i++)
                if (!IsOutlier(measurements[i].Nanoseconds, lowerFence, upperFence))
                    variance += Math.Pow(measurements[i].Nanoseconds - mean, 2) / (N - 1);

            return variance;
        }

        private static double GetQuartile(List<Measurement> measurements, int count)
        {
            if (count % 2 == 0)
                return (measurements[count / 2 - 1].Nanoseconds + measurements[count / 2].Nanoseconds) / 2;

            return measurements[count / 2].Nanoseconds;
        }

        private static bool IsOutlier(double value, double LowerFence, double UpperFence)
            => value < LowerFence || value > UpperFence;
    }
}
