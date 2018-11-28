using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics
{
    public class Statistics
    {
        private readonly List<double> originalValues, sortedValues;

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

        public Statistics(params double[] values) :
            this(values.ToList()) { }

        public Statistics(IEnumerable<int> values) :
            this(values.Select(value => (double) value)) { }

        public Statistics(IEnumerable<double> values)
        {
            originalValues = values.Where(d => !double.IsNaN(d)).ToList();
            sortedValues = originalValues.ToList();
            N = sortedValues.Count;
            if (N == 0)
                throw new InvalidOperationException("Sequence of values contains no elements, Statistics can't be calculated");
            sortedValues.Sort();

            if (N == 1)
                Q1 = Median = Q3 = sortedValues[0];
            else
            {
                double GetMedian(IList<double> x) => x.Count % 2 == 0
                    ? (x[x.Count / 2 - 1] + x[x.Count / 2]) / 2
                    : x[x.Count / 2];

                Median = GetMedian(sortedValues);
                Q1 = GetMedian(sortedValues.Take(N / 2).ToList());
                Q3 = GetMedian(sortedValues.Skip((N + 1) / 2).ToList());
            }

            Min = sortedValues.First();
            Mean = sortedValues.Average();
            Max = sortedValues.Last();

            InterquartileRange = Q3 - Q1;
            LowerFence = Q1 - 1.5 * InterquartileRange;
            UpperFence = Q3 + 1.5 * InterquartileRange;

            AllOutliers = sortedValues.Where(IsOutlier).ToArray();
            LowerOutliers = sortedValues.Where(IsLowerOutlier).ToArray();
            UpperOutliers = sortedValues.Where(IsUpperOutlier).ToArray();

            Variance = N == 1 ? 0 : sortedValues.Sum(d => Math.Pow(d - Mean, 2)) / (N - 1);
            StandardDeviation = Math.Sqrt(Variance);
            StandardError = StandardDeviation / Math.Sqrt(N);
            Skewness = CalcCentralMoment(3) / StandardDeviation.Pow(3);
            Kurtosis = CalcCentralMoment(4) / StandardDeviation.Pow(4);
            ConfidenceInterval = new ConfidenceInterval(Mean, StandardError, N);
            Percentiles = new PercentileValues(sortedValues);
        }

        [PublicAPI] public ConfidenceInterval GetConfidenceInterval(ConfidenceLevel level, int n) => new ConfidenceInterval(Mean, StandardError, n, level);
        [PublicAPI] public bool IsLowerOutlier(double value) => value < LowerFence;
        [PublicAPI] public bool IsUpperOutlier(double value) => value > UpperFence;
        [PublicAPI] public bool IsOutlier(double value) => value < LowerFence || value > UpperFence;
        [PublicAPI] public double[] WithoutOutliers() => sortedValues.Where(value => !IsOutlier(value)).ToArray();
        [PublicAPI] public IReadOnlyCollection<double> GetOriginalValues() => originalValues;
        [PublicAPI] public IReadOnlyCollection<double> GetSortedValues() => sortedValues;

        [PublicAPI] public double CalcCentralMoment(int k) => sortedValues.Average(x => (x - Mean).Pow(k));

        public bool IsActualOutlier(double value, OutlierMode outlierMode)
        {
            switch (outlierMode)
            {
                case OutlierMode.None:
                    return false;
                case OutlierMode.OnlyUpper:
                    return IsUpperOutlier(value);
                case OutlierMode.OnlyLower:
                    return IsLowerOutlier(value);
                case OutlierMode.All:
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
                case OutlierMode.None:
                    return Array.Empty<double>();
                case OutlierMode.OnlyUpper:
                    return UpperOutliers;
                case OutlierMode.OnlyLower:
                    return LowerOutliers;
                case OutlierMode.All:
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
                    return $"{sortedValues[0]} (N = 1)";
                case 2:
                    return $"{sortedValues[0]},{sortedValues[1]} (N = 2)";
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
        public Statistics Invert() => CanBeInverted() ? new Statistics(sortedValues.Select(x => 1 / x)) : null;

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
                if (Math.Abs(y.originalValues[i]) < 1e-9)
                    throw new DivideByZeroException($"y[{i}] is {y.originalValues[i]}");
                z[i] = x.originalValues[i] / y.originalValues[i];
            }

            return new Statistics(z);
        }
    }
}