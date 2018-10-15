using BenchmarkDotNet.Mathematics.StatisticalTesting;

namespace BenchmarkDotNet.Analysers
{
    public static class ZeroMeasurementHelper
    {
        /// <summary>
        /// Checks distribution against Zero Measurement hypothesis in case of known threshold
        /// </summary>
        /// <returns>True if measurement is ZeroMeasurement</returns>
        public static bool CheckZeroMeasurementOneSample(double[] results, double threshold) 
            => !StudentTest.Instance.IsGreater(results, threshold).NullHypothesisIsRejected;

        /// <summary>
        /// Checks distribution against Zero Measurement hypothesis in case of two samples
        /// </summary>
        /// <returns>True if measurement is ZeroMeasurement</returns>
        public static bool CheckZeroMeasurementTwoSamples(double[] workload, double[] overhead) 
            => !WelchTest.Instance.IsGreater(workload, overhead).NullHypothesisIsRejected;
    }
}