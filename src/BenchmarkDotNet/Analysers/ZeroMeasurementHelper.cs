using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace BenchmarkDotNet.Analysers
{
    public static class ZeroMeasurementHelper
    {
        /// <summary>
        /// Checks distribution against Zero Measurement hypothesis in case of known threshold
        /// </summary>
        /// <returns>True if measurement is ZeroMeasurement</returns>
        public static bool CheckZeroMeasurementOneSample(double[] results, double threshold)
        {
            if (results.Length < 3)
                return false;
            return !StudentTest.Instance.IsGreater(results, threshold).NullHypothesisIsRejected;
        }

        /// <summary>
        /// Checks distribution against Zero Measurement hypothesis in case of two samples
        /// </summary>
        /// <returns>True if measurement is ZeroMeasurement</returns>
        public static bool CheckZeroMeasurementTwoSamples(double[] workload, double[] overhead, Threshold? threshold = null)
        {
            if (workload.Length < 3 || overhead.Length < 3)
                return false;
            return !WelchTest.Instance.IsGreater(workload, overhead, threshold).NullHypothesisIsRejected;
        }
    }
}