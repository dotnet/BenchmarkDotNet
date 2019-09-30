using System;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class WelchTest : IOneSidedTest<WelchResult>
    {
        public static readonly WelchTest Instance = new WelchTest();

        /// <summary>
        /// Checks that (x-y) > threshold
        /// </summary>
        /// <remarks>Should be consistent with t.test(x, y, mu=threshold, alternative="greater") from R</remarks>
        [NotNull]
        public WelchResult IsGreater(double[] x, double[] y, Threshold threshold = null) => IsGreater(new Statistics(x), new Statistics(y), threshold);

        /// <summary>
        /// Checks that (x-y) > threshold
        /// </summary>
        /// <remarks>Should be consistent with t.test(x, y, mu=threshold, alternative="greater") from R</remarks>
        [NotNull]
        public WelchResult IsGreater(Statistics x, Statistics y, Threshold threshold = null)
        {
            int n1 = x.N, n2 = y.N;
            if (x.N < 2)
                throw new ArgumentException("x should contains at least 2 elements", nameof(x));
            if (y.N < 2)
                throw new ArgumentException("y should contains at least 2 elements", nameof(y));

            double v1 = x.Variance, v2 = y.Variance, m1 = x.Mean, m2 = y.Mean;

            threshold = threshold ?? RelativeThreshold.Default;
            double thresholdValue = threshold.GetValue(x);
            double se = Math.Sqrt(v1 / n1 + v2 / n2);
            double t = ((m1 - m2) - thresholdValue) / se;
            double df = (v1 / n1 + v2 / n2).Sqr() /
                        ((v1 / n1).Sqr() / (n1 - 1) + (v2 / n2).Sqr() / (n2 - 1));
            double pValue = 1 - MathHelper.StudentOneTail(t, df);

            return new WelchResult(t, df, pValue, threshold);
        }
    }
}