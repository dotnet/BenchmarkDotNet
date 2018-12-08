using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    /// <summary>
    /// Stopping criteria which require a specific amount of iterations.
    /// </summary>
    public class FixedStoppingCriteria : StoppingCriteriaBase
    {
        private readonly int iterationCount;

        private readonly string message;

        public FixedStoppingCriteria(int iterationCount)
        {
            this.iterationCount = iterationCount;

            message = $"The required amount of iteration ({iterationCount}) is achieved";
        }

        protected override string GetTitle() => $"{nameof(FixedStoppingCriteria)}({nameof(iterationCount)}={iterationCount})";

        protected override int GetMaxIterationCount() => iterationCount;

        protected override IEnumerable<string> GetWarnings()
        {
            if (iterationCount < 0)
                yield return $"Iteration count ({iterationCount}) is negative";
        }

        public override StoppingResolution Evaluate(IReadOnlyList<Measurement> measurements)
            => measurements.Count >= MaxIterationCount
                ? StoppingResolution.CreateFinished(message)
                : StoppingResolution.NotFinished;
    }
}