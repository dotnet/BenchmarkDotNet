using System;
using BenchmarkDotNet.Extensions;
using static System.Math;

namespace BenchmarkDotNet.Mathematics
{
    public class WelchTTest
    {
        public double T { get; }
        public double Df { get; }
        public double PValue { get; }
        public bool NullHypothesisIsRejected => PValue < 0.05;

        public WelchTTest(double x, double df, double pValue)
        {
            T = x;
            Df = df;
            PValue = pValue;
        }

        /// <summary>
        /// Welch's Two Sample t-test
        /// </summary>
        public static WelchTTest Calc(Statistics x, Statistics y)
        {
            int n1 = x.N, n2 = y.N;
            if (x.N < 2)
                throw new ArgumentException("x should contains at least 2 elements", nameof(x));
            if (y.N < 2)
                throw new ArgumentException("y should contains at least 2 elements", nameof(y));

            double v1 = x.Variance, v2 = y.Variance, m1 = x.Mean, m2 = y.Mean;

            double se = Sqrt(v1 / n1 + v2 / n2);
            double t = (m1 - m2) / se;
            double df = (v1 / n1 + v2 / n2).Sqr() /
                        ((v1 / n1).Sqr() / (n1 - 1) + (v2 / n2).Sqr() / (n2 - 1));
            double pValue = MathHelper.Student(t, df);

            return new WelchTTest(t, df, pValue);
        }
    }
}