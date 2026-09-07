using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// Disposes the parameter values of the benchmarks that were enumerated but will not be run.
    /// </summary>
    /// <remarks>
    /// Enumerating an assembly instantiates the values of every [Params], [ParamsSource] and [ArgumentsSource], and
    /// BenchmarkDotNet only disposes the ones belonging to the benchmarks it was handed. A value with a locking
    /// finalizer hangs the runtime when it is left to the finalizer thread instead, see dotnet/BenchmarkDotNet#1383,
    /// which is what makes this worse than an ordinary leak.
    /// </remarks>
    internal static class ParameterValueDisposer
    {
        /// <summary>
        /// Disposes every value that the enumerated benchmarks own and the retained ones do not.
        /// </summary>
        /// <remarks>
        /// The values are matched by reference instead of being disposed case by case, because BenchmarkConverter
        /// gives the same ParameterInstance to every job and every argument set of a benchmark: disposing a dropped
        /// case wholesale would take down values that a benchmark which is about to run still owns.
        /// </remarks>
        /// <param name="enumerated">Everything the assembly declares.</param>
        /// <param name="retained">The benchmarks that are kept, if any.</param>
        internal static void DisposeUnused(IEnumerable<BenchmarkCase> enumerated, IEnumerable<BenchmarkCase> retained)
        {
            var unused = new HashSet<IDisposable>(ReferenceComparer.Instance);

            foreach (var value in GetDisposableParameterValues(enumerated))
                unused.Add(value);

            foreach (var value in GetDisposableParameterValues(retained))
                unused.Remove(value);

            foreach (var value in unused)
                value.Dispose();
        }

        private static IEnumerable<IDisposable> GetDisposableParameterValues(IEnumerable<BenchmarkCase> benchmarkCases)
            => benchmarkCases
                .SelectMany(benchmarkCase => benchmarkCase.Parameters.Items)
                .Select(parameter => parameter.Value)
                .OfType<IDisposable>();

        /// <summary>
        /// Compares by reference, so that a parameter value which overrides Equals is still disposed once per instance.
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<IDisposable>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(IDisposable? x, IDisposable? y) => ReferenceEquals(x, y);

            public int GetHashCode(IDisposable obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
