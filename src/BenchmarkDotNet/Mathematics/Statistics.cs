using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Mathematics.QuantileEstimators;

namespace BenchmarkDotNet.Mathematics
{
    public class Statistics
    {
        public IReadOnlyList<double> OriginalValues { get; }
        internal IReadOnlyList<double> SortedValues { get; }
        public int N { get; }
        public double Min { get; }
        public double LowerFence { get; }
        public double Q1 { get; }
        public double Median { get; }
        public double Mean { get; }
        public double Q3 { get; }
        public double UpperFence { get; }
        public double Max { get; }
        public double InterquartileRange { get; }
        public double[] LowerOutliers { get; }
        public double[] UpperOutliers { get; }
        public double[] AllOutliers { get; }
        public double StandardError { get; }
        public double Variance { get; }
        public double StandardDeviation { get; }
        public double Skewness { get; }
        public double Kurtosis { get; }
        public ConfidenceInterval ConfidenceInterval { get; }
        public PercentileValues Percentiles { get; }

        private OutlierDetector outlierDetector;

        public Statistics(params double[] values) :
            this(values.ToList()) { }

        public Statistics(IEnumerable<int> values) :
            this(values.Select(value => (double) value)) { }

        public Statistics(IEnumerable<double> values)
        {
            OriginalValues = values.Where(d => !double.IsNaN(d)).ToArray();
            SortedValues = OriginalValues.OrderBy(value => value).ToArray();
            N = SortedValues.Count;
            if (N == 0)
                throw new InvalidOperationException("Sequence of values contains no elements, Statistics can't be calculated");

            var quartiles = Quartiles.FromSorted(SortedValues);
            Min = quartiles.Min;
            Q1 = quartiles.Q1;
            Median = quartiles.Median;
            Q3 = quartiles.Q3;
            Max = quartiles.Max;
            InterquartileRange = quartiles.InterquartileRange;

            var moments = Moments.Create(SortedValues);
            Mean = moments.Mean;
            StandardDeviation = moments.StandardDeviation;
            Variance = moments.Variance;
            Skewness = moments.Skewness;
            Kurtosis = moments.Kurtosis;

            var tukey = TukeyOutlierDetector.FromQuartiles(quartiles);
            LowerFence = tukey.LowerFence;
            UpperFence = tukey.UpperFence;
            AllOutliers = SortedValues.Where(tukey.IsOutlier).ToArray();
            LowerOutliers = SortedValues.Where(tukey.IsLowerOutlier).ToArray();
            UpperOutliers = SortedValues.Where(tukey.IsUpperOutlier).ToArray();
            outlierDetector = tukey;

            StandardError = StandardDeviation / Math.Sqrt(N);
            ConfidenceInterval = new ConfidenceInterval(Mean, StandardError, N);
            Percentiles = new PercentileValues(SortedValues);
        }

        [PublicAPI] public ConfidenceInterval GetConfidenceInterval(ConfidenceLevel level, int n) => new ConfidenceInterval(Mean, StandardError, n, level);
        [PublicAPI] public bool IsLowerOutlier(double value) => outlierDetector.IsLowerOutlier(value);
        [PublicAPI] public bool IsUpperOutlier(double value) => outlierDetector.IsUpperOutlier(value);
        [PublicAPI] public bool IsOutlier(double value) => outlierDetector.IsOutlier(value);
        [PublicAPI] public double[] WithoutOutliers() => SortedValues.Where(value => !IsOutlier(value)).ToArray();

        [PublicAPI] public double CalcCentralMoment(int k) => SortedValues.Average(x => (x - Mean).Pow(k));

        public bool IsActualOutlier(double value, OutlierMode outlierMode)
        {
            switch (outlierMode)
            {
                case OutlierMode.DontRemove:
                    return false;
                case OutlierMode.RemoveUpper:
                    return IsUpperOutlier(value);
                case OutlierMode.RemoveLower:
                    return IsLowerOutlier(value);
                case OutlierMode.RemoveAll:
                    return IsOutlier(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(outlierMode), outlierMode, null);
            }
        }

        [PublicAPI, NotNull]
        public double[] GetActualOutliers(OutlierMode outlierMode)
        {
            switch (outlierMode)
            {
                case OutlierMode.DontRemove:
                    return Array.Empty<double>();
                case OutlierMode.RemoveUpper:
                    return UpperOutliers;
                case OutlierMode.RemoveLower:
                    return LowerOutliers;
                case OutlierMode.RemoveAll:
                    return AllOutliers;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outlierMode), outlierMode, null);
            }
        }

        public override string ToString()
        {
            switch (N)
            {
                case 1:
                    return $"{SortedValues[0]} (N = 1)";
                case 2:
                    return $"{SortedValues[0]},{SortedValues[1]} (N = 2)";
                default:
                    return $"{Mean} +- {ConfidenceInterval.Margin} (N = {N})";
            }
        }

        /// <summary>
        /// Returns true, if this statistics can be inverted (see <see cref="Invert"/>).
        /// </summary>
        public bool CanBeInverted() => Min > 1e-9;

        /// <summary>
        /// Statistics for [1/X]. If Min is less then or equal to 0, returns null.
        /// </summary>
        public Statistics Invert() => CanBeInverted() ? new Statistics(SortedValues.Select(x => 1 / x)) : null;

        /// <summary>
        /// Mean for [X*Y].
        /// </summary>
        public static double MulMean(Statistics x, Statistics y) => x.Mean * y.Mean;

        /// <summary>
        /// Mean for [X/Y].
        /// </summary>
        public static double DivMean([CanBeNull] Statistics x, [CanBeNull] Statistics y)
        {
            if (x == null || y == null)
                return double.NaN;
            var yInvert = y.Invert();
            if (yInvert == null)
                throw new DivideByZeroException();
            return MulMean(x, yInvert);
        }

        [NotNull]
        public static Statistics Divide([NotNull] Statistics x, [NotNull] Statistics y)
        {
            if (x.N < 1)
                throw new ArgumentOutOfRangeException(nameof(x), "Argument doesn't contain any values");
            if (y.N < 1)
                throw new ArgumentOutOfRangeException(nameof(y), "Argument doesn't contain any values");
            int n = Math.Min(x.N, y.N);
            var z = new double[n];
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(y.OriginalValues[i]) < 1e-9)
                    throw new DivideByZeroException($"y[{i}] is {y.OriginalValues[i]}");
                z[i] = x.OriginalValues[i] / y.OriginalValues[i];
            }

            return new Statistics(z);
        }
    }
}