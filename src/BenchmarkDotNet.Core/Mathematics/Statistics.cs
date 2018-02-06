using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Mathematics
{
    public class Statistics
    {
        private readonly List<double> list;

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
        public double[] Outliers { get; }
        public double StandardError { get; }
        public double Variance { get; }
        public double StandardDeviation { get; }
        public double Skewness { get; }
        public double Kurtosis { get; }
        public ConfidenceInterval ConfidenceInterval { get; }
        public PercentileValues Percentiles { get; }

        public Statistics(params double[] values) :
            this(values.ToList())
        {
        }

        public Statistics(IEnumerable<int> values) :
            this(values.Select(value => (double) value))
        {
        }

        public Statistics(IEnumerable<double> values)
        {
            list = values.ToList();
            N = list.Count;
            if (N == 0)
                throw new InvalidOperationException("Sequence of values contains no elements, Statistics can't be calculated");
            list.Sort();

            if (N == 1)
                Q1 = Median = Q3 = list[0];
            else
            {
                Func<IList<double>, double> getMedian = x => x.Count % 2 == 0
                    ? (x[x.Count / 2 - 1] + x[x.Count / 2]) / 2
                    : x[x.Count / 2];
                Median = getMedian(list);
                Q1 = getMedian(list.Take(N / 2).ToList());
                Q3 = getMedian(list.Skip((N + 1) / 2).ToList());
            }

            Min = list.First();
            Mean = list.Average();
            Max = list.Last();

            InterquartileRange = Q3 - Q1;
            LowerFence = Q1 - 1.5 * InterquartileRange;
            UpperFence = Q3 + 1.5 * InterquartileRange;

            Outliers = list.Where(IsOutlier).ToArray();

            Variance = N == 1 ? 0 : list.Sum(d => Math.Pow(d - Mean, 2)) / (N - 1);
            StandardDeviation = Math.Sqrt(Variance);
            StandardError = StandardDeviation / Math.Sqrt(N);
            Skewness = CalcCentralMoment(3) / StandardDeviation.Pow(3);
            Kurtosis = CalcCentralMoment(4) / StandardDeviation.Pow(4);
            ConfidenceInterval = new ConfidenceInterval(Mean, StandardError, N);
            Percentiles = new PercentileValues(list);
        }

        public ConfidenceInterval GetConfidenceInterval(ConfidenceLevel level, int n) => new ConfidenceInterval(Mean, StandardError, n, level);
        public bool IsOutlier(double value) => value < LowerFence || value > UpperFence;
        public double[] WithoutOutliers() => list.Where(value => !IsOutlier(value)).ToArray();
        public IEnumerable<double> GetValues() => list;

        public double CalcCentralMoment(int k) => list.Average(x => (x - Mean).Pow(k));

        public override string ToString()
        {
            switch (N)
            {
                case 1:
                    return $"{list[0]} (N = 1)";
                case 2:
                    return $"{list[0]},{list[1]} (N = 2)";
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
        public Statistics Invert() => CanBeInverted() ? new Statistics(list.Select(x => 1 / x)) : null;

        /// <summary>
        /// Statistics for [X^2].
        /// </summary>        
        public Statistics Sqr() => new Statistics(list.Select(x => x * x));

        /// <summary>
        /// Mean for [X*Y].
        /// </summary>        
        public static double MulMean(Statistics x, Statistics y) => x.Mean * y.Mean;

        /// <summary>
        /// Mean for [X/Y].
        /// </summary>        
        public static double DivMean(Statistics x, Statistics y)
        {
            var yInvert = y.Invert();
            if (yInvert == null)
                throw new DivideByZeroException();
            return MulMean(x, yInvert);
        }

        /// <summary>
        /// Variance for [X*Y].
        /// </summary>        
        public static double MulVariance(Statistics x, Statistics y)
        {
            return x.Sqr().Mean * y.Sqr().Mean - x.Mean.Sqr() * y.Mean.Sqr();
        }

        /// <summary>
        /// Variance for [X/Y].
        /// </summary>        
        public static double DivVariance(Statistics x, Statistics y)
        {
            var yInvert = y.Invert();
            if (yInvert == null)
                throw new DivideByZeroException();
            return MulVariance(x, yInvert);
        }
    }
}