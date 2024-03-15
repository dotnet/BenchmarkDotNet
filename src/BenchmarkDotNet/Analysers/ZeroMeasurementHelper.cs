using BenchmarkDotNet.Mathematics;
using Perfolizer;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.GenericEstimators;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;

namespace BenchmarkDotNet.Analysers
{
    internal static class ZeroMeasurementHelper
    {
        public static bool IsNegligible(Sample results, double threshold) => HodgesLehmannEstimator.Instance.Median(results) < threshold;
        public static bool IsNoticeable(Sample results, double threshold) => !IsNegligible(results, threshold);

        public static bool AreIndistinguishable(double[] workload, double[] overhead, Threshold? threshold = null)
        {
            var workloadSample = new Sample(workload, TimeUnit.Nanosecond);
            var overheadSample = new Sample(overhead, TimeUnit.Nanosecond);
            return AreIndistinguishable(workloadSample, overheadSample, threshold);
        }

        public static bool AreIndistinguishable(Sample workload, Sample overhead, Threshold? threshold = null)
        {
            threshold ??= MathHelper.DefaultThreshold;
            var tost = new SimpleEquivalenceTest(MannWhitneyTest.Instance);
            if (workload.Size == 1 || overhead.Size == 1)
                return false;
            return tost.Perform(workload, overhead, threshold, SignificanceLevel.P1E5) == ComparisonResult.Indistinguishable;
        }

        public static bool AreDistinguishable(double[] workload, double[] overhead, Threshold? threshold = null) =>
            !AreIndistinguishable(workload, overhead, threshold);

        public static bool AreDistinguishable(Sample workload, Sample overhead, Threshold? threshold = null) =>
            !AreIndistinguishable(workload, overhead, threshold);
    }
}