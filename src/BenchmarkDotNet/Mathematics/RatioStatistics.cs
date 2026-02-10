using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Mathematics
{
    /// <summary>
    /// Statistics for *relation* of two data sets
    /// </summary>
    public class RatioStatistics
    {
        public double Mean { get; }
        public double StandardDeviation { get; }

        public RatioStatistics(Statistics x, Statistics y)
        {
            if (x.N < 1)
                throw new ArgumentOutOfRangeException(nameof(x), "Argument doesn't contain any values");
            if (y.N < 1)
                throw new ArgumentOutOfRangeException(nameof(y), "Argument doesn't contain any values");

            if (x == y)
            {
                Mean = 1.0;
                StandardDeviation = 0.0;
            }
            else
            {
                var divided = Divide(x, y);
                Mean = divided.Mean;
                StandardDeviation = divided.StandardDeviation;
            }
        }

        private static Statistics Divide(Statistics x, Statistics y)
        {
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
