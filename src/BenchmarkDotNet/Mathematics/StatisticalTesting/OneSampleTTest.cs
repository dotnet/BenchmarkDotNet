using System;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class OneSampleTTest
    {
        public static readonly OneSampleTTest Instance = new OneSampleTTest();
        
        /// <summary>
        /// Determines whether the sample mean is different from a known mean
        /// </summary>
        public OneSidedTestResult IsGreater(double[] sample, double value, Threshold threshold = null) 
            => IsGreater(new Statistics(sample), value, threshold);
        
        /// <summary>
        /// Determines whether the sample mean is different from a known mean
        /// </summary>
        public OneSidedTestResult IsGreater(Statistics sample, double value, Threshold threshold = null)
        {
            double mean = sample.Mean;
            double stdDev = sample.StandardDeviation;
            double n = sample.N;
            double df = n - 1;
            
            threshold = threshold ?? RelativeThreshold.Default;

            double t = (mean - value) / 
                       (stdDev * Math.Sqrt(n));
            double pValue = 1 - MathHelper.StudentOneTail(t, df);
            
            return new OneSidedTestResult(pValue, threshold);
        }
    }
}