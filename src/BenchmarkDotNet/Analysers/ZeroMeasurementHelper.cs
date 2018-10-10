using System;
using BenchmarkDotNet.Mathematics.StatisticalTesting;

namespace BenchmarkDotNet.Analysers
{
    public class ZeroMeasurementHelper
    {
        /// <summary>
        /// Check distribution against Zero Measurement hypothesis
        /// Null hypothesis - distribution is not Zero Measurement distribution
        /// </summary>
        /// <returns></returns>
        public static bool CheckZeroMeasurement(double[] results, double threshold)
        {
            var sample = new [] { threshold, threshold };
            Console.WriteLine($"-----threshold = {threshold}-----");
            foreach (double result in results)
            {
                Console.WriteLine(result);
            }
            Console.WriteLine("----------------------------------");
            return !WelchTest.Instance.IsGreater(results, sample).NullHypothesisIsRejected;
        }
    }
}