using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;
using Perfolizer;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Mathematics.QuantileEstimators;

namespace BenchmarkDotNet.Mathematics
{
    public class Statistics
    {
        internal Sample Sample { get; }
        public IReadOnlyList<double> OriginalValues { get; }
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
        private ConfidenceIntervalEstimator ConfidenceIntervalEstimator { get; }
        internal ConfidenceInterval PerfolizerConfidenceInterval { get; }
        public LegacyConfidenceInterval ConfidenceInterval { get; }
        public PercentileValues Percentiles { get; }

        private readonly TukeyOutlierDetector outlierDetector;

        public Statistics(params double[] values) :
            this(values.ToList()) { }

        public Statistics(IEnumerable<int> values) :
            this(values.Select(value => (double)value)) { }

        public Statistics(IEnumerable<double> values) : this(new Sample(values.ToArray(), TimeUnit.Nanosecond)) { }

        public Statistics(Sample sample)
        {
            Sample = sample;
            OriginalValues = sample.Values;
            N = Sample.Size;

            var quartiles = Quartiles.Create(Sample);
            Min = quartiles.Min;
            Q1 = quartiles.Q1;
            Median = quartiles.Median;
            Q3 = quartiles.Q3;
            Max = quartiles.Max;
            InterquartileRange = quartiles.InterquartileRange;

            var moments = Moments.Create(Sample);
            Mean = moments.Mean;
            StandardDeviation = moments.StandardDeviation;
            Variance = moments.Variance;
            Skewness = moments.Skewness;
            Kurtosis = moments.Kurtosis;

            var tukey = TukeyOutlierDetector.Create(Sample);
            LowerFence = tukey.LowerFence;
            UpperFence = tukey.UpperFence;
            AllOutliers = Sample.SortedValues.Where(tukey.IsOutlier).ToArray();
            LowerOutliers = Sample.SortedValues.Where(tukey.IsLowerOutlier).ToArray();
            UpperOutliers = Sample.SortedValues.Where(tukey.IsUpperOutlier).ToArray();
            outlierDetector = tukey;

            StandardError = StandardDeviation / Math.Sqrt(N);
            ConfidenceIntervalEstimator = new ConfidenceIntervalEstimator(Sample.Size, Mean, StandardError);
            PerfolizerConfidenceInterval = ConfidenceIntervalEstimator.ConfidenceInterval(ConfidenceLevel.L999);
            ConfidenceInterval = new LegacyConfidenceInterval(PerfolizerConfidenceInterval.Estimation, StandardError, N, LegacyConfidenceLevel.L999);
            Percentiles = new PercentileValues(Sample.SortedValues);
        }

        [PublicAPI] public ConfidenceInterval GetConfidenceInterval(ConfidenceLevel level) => ConfidenceIntervalEstimator.ConfidenceInterval(level);
        [PublicAPI] public bool IsLowerOutlier(double value) => outlierDetector.IsLowerOutlier(value);
        [PublicAPI] public bool IsUpperOutlier(double value) => outlierDetector.IsUpperOutlier(value);
        [PublicAPI] public bool IsOutlier(double value) => outlierDetector.IsOutlier(value);
        [PublicAPI] public double[] WithoutOutliers() => outlierDetector.WithoutAllOutliers(Sample.Values).ToArray();

        [PublicAPI] public double CalcCentralMoment(int k) => Sample.SortedValues.Average(x => (x - Mean).Pow(k));

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

        [PublicAPI]
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

        public override string ToString() => Sample.ToString();

        /// <summary>
        /// Returns true, if this statistics can be inverted (see <see cref="Invert"/>).
        /// </summary>
        public bool CanBeInverted() => Min > 1e-9;

        /// <summary>
        /// Statistics for [1/X]. If Min is less then or equal to 0, returns null.
        /// </summary>
        public Statistics Invert() => CanBeInverted() ? new Statistics(Sample.SortedValues.Select(x => 1 / x)) : null;

        /// <summary>
        /// Mean for [X*Y].
        /// </summary>
        public static double MulMean(Statistics x, Statistics y) => x.Mean * y.Mean;

        /// <summary>
        /// Mean for [X/Y].
        /// </summary>
        public static double DivMean(Statistics? x, Statistics? y)
        {
            if (x == null || y == null)
                return double.NaN;
            var yInvert = y.Invert();
            if (yInvert == null)
                throw new DivideByZeroException();
            return MulMean(x, yInvert);
        }

        public static Statistics Divide(Statistics x, Statistics y)
        {
            if (x.N < 1)
                throw new ArgumentOutOfRangeException(nameof(x), "Argument doesn't contain any values");
            if (y.N < 1)
                throw new ArgumentOutOfRangeException(nameof(y), "Argument doesn't contain any values");

            double[]? z = new double[x.N * y.N];
            int k = 0;
            for (int i = 0; i < x.N; i++)
            for (int j = 0; j < y.N; j++)
            {
                if (Math.Abs(y.Sample.Values[j]) < 1e-9)
                    throw new DivideByZeroException($"y[{j}] is {y.Sample.Values[j]}");
                z[k++] = x.Sample.Values[i] / y.Sample.Values[j];
            }

            return new Statistics(z);
        }
    }
}