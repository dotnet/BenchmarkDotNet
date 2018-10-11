using BenchmarkDotNet.Mathematics.StatisticalTesting;

namespace BenchmarkDotNet.Analysers
{
    public class ZeroMeasurementHelper
    {
        /// <summary>
        /// Checks distribution against Zero Measurement hypothesis
        /// </summary>
        /// <returns>True if measurement is ZeroMeasurement</returns>
        public static bool CheckZeroMeasurement(double[] results, double threshold)
        {
            var sample = new [] { threshold, threshold };
            return !WelchTest.Instance.IsGreater(results, sample).NullHypothesisIsRejected;
        }
    }
}