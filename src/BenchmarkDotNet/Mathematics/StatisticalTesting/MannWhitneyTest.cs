using System;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class MannWhitneyTest : IOneSidedTest<MannWhitneyResult>
    {
        public static readonly MannWhitneyTest Instance = new MannWhitneyTest();

        private static double PValueForSmallN(int n, int m, double u)
        {
            int q = (int) Math.Floor(u + 1e-9);
            int nm = Math.Max(n, m);
            var w = new long[nm + 1, nm + 1, q + 1];
            for (int i = 0; i <= nm; i++)
            for (int j = 0; j <= nm; j++)
            for (int k = 0; k <= q; k++)
            {
                if (i == 0 || j == 0 || k == 0)
                    w[i, j, k] = k == 0 ? 1 : 0;
                else if (k > i * j)
                    w[i, j, k] = 0;
                else if (i > j)
                    w[i, j, k] = w[j, i, k];
                else if (j > 0 && k < j)
                    w[i, j, k] = w[i, k, k];
                else
                    w[i, j, k] = w[i - 1, j, k - j] + w[i, j - 1, k];
            }

            long denominator = BinomialCoefficientHelper.GetBinomialCoefficient(n + m, m);
            long p = 0;
            if (q <= n * m / 2)
            {
                for (int i = 0; i <= q; i++)
                    p += w[n, m, i];
            }
            else
            {
                q = n * m - q;
                for (int i = 0; i < q; i++)
                    p += w[n, m, i];
                p = denominator - p;
            }

            return p * 1.0 / denominator;
        }

        /// <summary>
        /// Checks that (x-y) > threshold
        /// </summary>
        /// <remarks>Should be consistent with wilcox.test(x, y, mu=threshold, alternative="greater") from R</remarks>
        public MannWhitneyResult IsGreater(double[] x, double[] y, Threshold threshold = null)
        {
            threshold = threshold ?? RelativeThreshold.Default;
            double thresholdValue = threshold.GetValue(new Statistics(x));

            int n = x.Length, m = y.Length;
            if (Math.Min(n, m) < 3 || Math.Max(n, m) < 5)
                return null; // Test can't be applied

            var xy = new double[n + m];
            for (int i = 0; i < n; i++)
                xy[i] = x[i];
            for (int i = 0; i < m; i++)
                xy[n + i] = y[i] + thresholdValue;
            var index = new int[n + m];
            for (int i = 0; i < n + m; i++)
                index[i] = i;
            Array.Sort(index, (i, j) => xy[i].CompareTo(xy[j]));

            var ranks = new double[n + m];
            for (int i = 0; i < n + m;)
            {
                int j = i;
                while (j + 1 < n + m && Math.Abs(xy[index[j + 1]] - xy[index[i]]) < 1e-9)
                    j++;
                double rank = (i + j + 2) / 2.0;
                for (int k = i; k <= j; k++)
                    ranks[k] = rank;
                i = j + 1;
            }

            double ux = 0;
            for (int i = 0; i < n + m; i++)
                if (index[i] < n)
                    ux += ranks[i];
            ux -= n * (n + 1) / 2.0;
            double uy = n * m - ux;

            if (n + m <= BinomialCoefficientHelper.MaxAcceptableN)
            {
                double pValue = 1 - PValueForSmallN(n, m, ux - 1);
                return new MannWhitneyResult(ux, uy, pValue, threshold);
            }
            else
            {
                double mu = n * m / 2.0;
                double su = Math.Sqrt(n * m * (n + m + 1) / 12.0);
                double z = (ux - mu) / su;
                double pValue = 1 - MathHelper.Gauss(z);
                return new MannWhitneyResult(ux, uy, pValue, threshold);
            }
        }
    }
}