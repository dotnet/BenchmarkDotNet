using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    /// <summary>
    /// A stopping criteria checks when it's time to terminate iteration in the current stage.
    /// </summary>
    public interface IStoppingCriteria
    {
        /// <summary>
        /// Checks do we have enough iterations
        /// </summary>
        StoppingResult Evaluate(IReadOnlyList<Measurement> measurements);

        /// <summary>
        /// Title which can be used in logs and diagnostics methods
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The maximum possible count of iterations.
        /// Engine needs this value for setting the maximum capacity of the returned list of measurements.
        /// The correct capacity helps to avoid infrastructure allocations during benchmarking.
        /// </summary>
        int MaxIterationCount { get; }

        /// <summary>
        /// An array of user-friendly warnings which notify about incorrect parameters.
        /// </summary>
        IReadOnlyList<string> Warnings { get; }
    }
}