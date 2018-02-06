using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics.Histograms;
using JetBrains.Annotations;
using static System.Math;

namespace BenchmarkDotNet.Mathematics
{
    public static class MathHelper
    {
        /// <summary>
        /// ACM Algorithm 209: Gauss
        /// 
        /// Calculates $(1/\sqrt{2\pi}) \int_{-\infty}^x e^{-u^2 / 2} du$
        /// by means of polynomial approximations due to A. M. Murray of Aberdeen University;
        /// 
        /// See: http://dl.acm.org/citation.cfm?id=367664
        /// </summary>        
        /// <param name="x">-infinity..+infinity</param>        
        /// <returns>Area under the Standard Normal Curve from -infinity to x</returns>
        public static double Gauss(double x)
        {
            double z;
            if (Abs(x) < 1e-9)
                z = 0.0;
            else
            {
                double y = Abs(x) / 2;
                if (y >= 3.0)
                    z = 1.0;
                else if (y < 1.0)
                {
                    double w = y * y;
                    z = ((((((((0.000124818987 * w - 0.001075204047) * w + 0.005198775019) * w - 0.019198292004) * w +
                             0.059054035642) * w - 0.151968751364) * w + 0.319152932694) * w - 0.531923007300) * w +
                         0.797884560593) * y * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    z = (((((((((((((-0.000045255659 * y + 0.000152529290) * y - 0.000019538132) * y - 0.000676904986) *
                                  y + 0.001390604284) * y - 0.000794620820) * y - 0.002034254874) * y + 0.006549791214) *
                              y - 0.010557625006) * y + 0.011630447319) * y - 0.009279453341) * y + 0.005353579108) * y -
                          0.002141268741) * y + 0.000535310849) * y + 0.999936657524;
                }
            }

            return x > 0.0 ? (z + 1.0) / 2 : (1.0 - z) / 2;
        }


        /// <summary>
        /// ACM Algorithm 395: Student's t-distribution
        /// 
        /// Evaluates the two-tail probability P(t|n) that t is exceeded 
        /// in magnitude for Student's t-distribution with n degrees of freedom.
        /// 
        /// http://dl.acm.org/citation.cfm?id=355599
        /// </summary>
        /// <param name="t">t-value</param>
        /// <param name="n">Degree of freedom, n >= 20</param>
        /// <returns>2-tail p-value</returns>
        public static double Student(double t, double n)
        {
            t = t.Sqr();
            double y = t / n;
            double b = y + 1.0;
            if (y > 1.0e-6)
                y = Log(b);
            double a = n - 0.5;
            b = 48.0 * a.Sqr();
            y = a * y;
            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) / (0.8 * y.Sqr() + 100.0 + b) + y + 3.0) / b + 1.0) * Sqrt(y);
            return 2.0 * Gauss(-y);
        }

        // TODO: Optimize
        public static double InverseStudent(double p, double n)
        {
            double lower = 0.0;
            double upper = 1000.0;
            while (upper - lower > 1e-9)
            {
                double t = (lower + upper) / 2;
                double p2 = Student(t, n);
                if (p2 < p)
                    upper = t;
                else
                    lower = t;
            }

            return (lower + upper) / 2;
        }

        // See http://www.brendangregg.com/FrequencyTrails/modes.html
        [PublicAPI]
        public static double CalculateMValue([NotNull] Statistics originalStatistics)
        {
            try
            {
                var s = new Statistics(originalStatistics.WithoutOutliers());

                double mValue = 0;

                double binSize = s.GetOptimalBinSize();
                if (Abs(binSize) < 1e-9)
                    binSize = 1;
                while (true)
                {
                    var histogram = HistogramBuilder.Adaptive.BuildWithFixedBinSize(s.GetValues(), binSize);
                    var x = new List<int>();
                    x.Add(0);
                    x.AddRange(histogram.Bins.Select(bin => bin.Count));
                    x.Add(0);

                    int sum = 0;
                    for (int i = 1; i < x.Count; i++)
                        sum += Abs(x[i] - x[i - 1]);
                    mValue = Max(mValue, sum * 1.0 / x.Max());

                    if (binSize > s.Max - s.Min)
                        break;
                    binSize *= 2.0;
                }

                return mValue;
            }
            catch (Exception)
            {
                return 1; // In case of any bugs, we return 1 because it's an invalud value (mvalue is always >= 2)
            }
        }

        public static int Clamp(int value, int min, int max) => Min(Max(value, min), max);
    }
}