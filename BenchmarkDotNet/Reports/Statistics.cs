using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Reports
{
    public class Statistics
    {
        public Statistics(params double[] values) :
            this(values.ToList())
        {
        }

        public Statistics(IEnumerable<int> values) :
            this(values.Select(value => (double)value))
        {
        }

        public Statistics(IEnumerable<double> values)
        {
            var list = values.ToList();
            N = list.Count;
            if (N == 0)
                throw new InvalidOperationException("StatSummary: Sequence contains no elements");
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

            Outlier = list.Where(value => value < LowerFence || value > UpperFence).ToArray();

            StandardDeviation = N == 1 ? 0 : Math.Sqrt(list.Sum(d => Math.Pow(d - Mean, 2)) / (N - 1));
            StandardError = StandardDeviation / Math.Sqrt(N);
            ConfidenceInterval = new ConfidenceInterval(Mean, StandardError);
        }

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
        public double[] Outlier { get; }
        public double StandardError { get; }
        public double StandardDeviation { get; }
        public ConfidenceInterval ConfidenceInterval { get; }

        public override string ToString() => $"{Mean} +- {StandardError} (N = {N})";
    }
}