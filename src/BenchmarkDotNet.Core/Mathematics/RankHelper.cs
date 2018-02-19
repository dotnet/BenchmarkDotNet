using System;
using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics
{
    internal static class RankHelper
    {
        public static int[] GetRanks([ItemNotNull] params Statistics[] stats)
        {
            var values = stats.
                Select((s, index) => new { Stats = s, Index = index }).
                OrderBy(pair => pair.Stats.Mean).
                ToArray();

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
            if (x.N >= 3 && y.N >= 3)
                return !WelchTTest.Calc(x, y).NullHypothesisIsRejected;
            return Math.Abs(x.Mean - y.Mean) < Math.Abs(x.Mean + y.Mean) / 2 * 0.01;
        }
    }
}