using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    /// <summary>
    /// Automatically choose the best number of iterations during the warmup stage.
    /// </summary>
    public class AutoWarmupStoppingCriteria : StoppingCriteriaBase
    {
        private const int DefaultMinFluctuationCount = 4;

        private readonly int minIterationCount;
        private readonly int maxIterationCount;
        private readonly int minFluctuationCount;

        private readonly string maxIterationMessage;
        private readonly string minFluctuationMessage;

        /// <summary>
        /// The idea of the implementation is simple: if measurements monotonously decrease or increase, the steady state is not achieved;
        /// we should continue the warmup stage. The evaluation method counts "fluctuations"
        /// (3 consecutive measurements A,B,C where (A&gt;B AND B&lt;C) OR (A&lt;B AND B&gt;C)) until the required amount of flotations is observed.
        /// </summary>
        /// <param name="minIterationCount">
        /// The minimum number of iterations.
        /// We are always going to do at least <paramref name="minIterationCount"/> iterations regardless of fluctuations.
        /// </param>
        /// <param name="maxIterationCount">
        /// The maximum number of iterations.
        /// We are always going to do at most <paramref name="maxIterationCount"/> iterations regardless of fluctuations.
        /// </param>
        /// <param name="minFluctuationCount">
        /// The required number of fluctuations.
        /// If the required number of fluctuations is achieved but the number of iterations less than <paramref name="minIterationCount"/>,
        /// we need more iterations.
        /// If the required number of fluctuations is not achieved but the number of iterations equal to <paramref name="maxIterationCount"/>,
        /// we should stop the iterations.
        /// </param>
        public AutoWarmupStoppingCriteria(int minIterationCount, int maxIterationCount, int minFluctuationCount = DefaultMinFluctuationCount)
        {
            this.minIterationCount = minIterationCount;
            this.maxIterationCount = maxIterationCount;
            this.minFluctuationCount = minFluctuationCount;

            maxIterationMessage = $"The maximum amount of iteration ({maxIterationCount}) is achieved";
            minFluctuationMessage = $"The minimum amount of fluctuation ({minFluctuationCount}) and " +
                                    $"the minimum amount of iterations ({minIterationCount}) are achieved";
        }

        public override StoppingResult Evaluate(IReadOnlyList<Measurement> measurements)
        {
            int n = measurements.Count;

            if (n >= maxIterationCount)
                return StoppingResult.CreateFinished(maxIterationMessage);
            if (n < minIterationCount)
                return StoppingResult.NotFinished;

            int direction = -1; // The default "pre-state" is "decrease mode"
            int fluctuationCount = 0;
            for (int i = 1; i < n; i++)
            {
                int nextDirection = Math.Sign(measurements[i].Nanoseconds - measurements[i - 1].Nanoseconds);
                if (nextDirection != direction || nextDirection == 0)
                {
                    direction = nextDirection;
                    fluctuationCount++;
                }
            }

            return fluctuationCount >= minFluctuationCount
                ? StoppingResult.CreateFinished(minFluctuationMessage)
                : StoppingResult.NotFinished;
        }

        protected override string GetTitle() => $"{nameof(AutoWarmupStoppingCriteria)}(" +
                                                $"{nameof(minIterationCount)}={minIterationCount}, " +
                                                $"{nameof(maxIterationCount)}={maxIterationCount}, " +
                                                $"{nameof(minFluctuationCount)}={minFluctuationCount})";

        protected override int GetMaxIterationCount() => maxIterationCount;

        protected override IEnumerable<string> GetWarnings()
        {
            if (minIterationCount < 0)
                yield return $"Min Iteration Count ({minIterationCount}) is negative";
            if (maxIterationCount < 0)
                yield return $"Max Iteration Count ({maxIterationCount}) is negative";
            if (minFluctuationCount < 0)
                yield return $"Min Fluctuation Count ({minFluctuationCount}) is negative";
            if (minIterationCount > maxIterationCount)
                yield return $"Min Iteration Count ({minFluctuationCount}) is greater than Max Iteration Count ({maxIterationCount})";
        }
    }
}