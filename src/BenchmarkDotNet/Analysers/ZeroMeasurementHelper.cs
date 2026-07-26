using BenchmarkDotNet.Mathematics;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;
using Pragmastat;
using Pragmastat.Estimators;
using Threshold = Perfolizer.Metrology.Threshold;

namespace BenchmarkDotNet.Analysers
{
    internal static class ZeroMeasurementHelper
    {
        public static bool IsNegligible(Sample results, double threshold) => CenterEstimator.Instance.Estimate(results) < threshold;
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
            // Perfolizer deprecated its significance testing API in favor of Pragmastat.Toolkit.Compare2.
            // Compare2 reports per-metric verdicts against typed thresholds instead of a single
            // equivalence result, so adopting it would change every verdict this codebase produces:
            // zero-measurement detection, ranks, and the statistical test column. Staying on the
            // deprecated path keeps the current statistics intact; the switch needs its own change,
            // with its own validation against real runs.
#pragma warning disable CS0618 // Type or member is obsolete
            var tost = new SimpleEquivalenceTest(MannWhitneyTest.Instance);
#pragma warning restore CS0618
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