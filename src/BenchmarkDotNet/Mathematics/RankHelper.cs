using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;

namespace BenchmarkDotNet.Mathematics
{
    internal static class RankHelper
    {
        public static int[] GetRanks(params Statistics[] stats)
        {
            var values = stats.Select((s, index) => new { Stats = s, Index = index }).OrderBy(pair => pair.Stats.Mean).ToArray();

            int n = values.Length;
            var ranks = new int[n];
            if (n > 0)
            {
                int currentRank = 1;
                ranks[values[0].Index] = currentRank;
                for (int i = 1; i < n; i++)
                {
                    if (AreSame(values[i - 1].Stats, values[i].Stats))
                        ranks[values[i].Index] = currentRank;
                    else
                        ranks[values[i].Index] = ++currentRank;
                }
            }
            return ranks;
        }

        private static bool AreSame(Statistics x, Statistics y)
        {
            // See ZeroMeasurementHelper: moving to Pragmastat.Toolkit.Compare2 would change the
            // computed ranks, so it needs its own change rather than riding along with a bump.
#pragma warning disable CS0618 // Type or member is obsolete
            var test = new SimpleEquivalenceTest(MannWhitneyTest.Instance);
#pragma warning restore CS0618
            var comparisonResult = test.Perform(x.Sample, y.Sample, MathHelper.DefaultThreshold, MathHelper.DefaultSignificanceLevel);
            return comparisonResult == ComparisonResult.Indistinguishable;
        }
    }
}