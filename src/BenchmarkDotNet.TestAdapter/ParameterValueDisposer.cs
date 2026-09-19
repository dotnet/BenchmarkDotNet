using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
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
        /// <inheritdoc cref="DisposeUnusedAsync"/>
        internal static void DisposeUnused(IEnumerable<BenchmarkCase> enumerated, IEnumerable<BenchmarkCase> retained)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            context.ExecuteUntilComplete(DisposeUnusedAsync(enumerated, retained));
        }

        /// <summary>
        /// Disposes every value that the enumerated benchmarks own and the retained ones do not.
        /// </summary>
        /// <remarks>
        /// The values are matched by reference instead of being disposed case by case, because BenchmarkConverter
        /// gives the same ParameterInstance to every job and every argument set of a benchmark: disposing a dropped
        /// case wholesale would take down values that a benchmark which is about to run still owns. Disposal goes
        /// through ParameterInstance so that a value which is only IAsyncDisposable is disposed as well.
        /// </remarks>
        /// <param name="enumerated">Everything the assembly declares.</param>
        /// <param name="retained">The benchmarks that are kept, if any.</param>
        internal static ValueTask DisposeUnusedAsync(IEnumerable<BenchmarkCase> enumerated, IEnumerable<BenchmarkCase> retained)
        {
            var unused = new Dictionary<object, ParameterInstance>(ReferenceComparer.Instance);

            foreach (var parameter in GetDisposableParameters(enumerated))
                unused[parameter.Value!] = parameter;

            foreach (var parameter in GetDisposableParameters(retained))
                unused.Remove(parameter.Value!);

            return unused.Values.DisposeAllAsync();
        }

        /// <summary>
        /// Compares values by reference, so that a parameter value which overrides Equals is still disposed once per
        /// instance.
        /// </summary>
        internal static IEqualityComparer<object> ByReference => ReferenceComparer.Instance;

        /// <summary>
        /// Gets the parameters of the given benchmarks whose value needs disposing.
        /// </summary>
        /// <param name="benchmarkCases">The benchmarks to read the parameters of.</param>
        /// <returns>The parameters holding a disposable value.</returns>
        internal static IEnumerable<ParameterInstance> GetDisposableParameters(IEnumerable<BenchmarkCase> benchmarkCases)
            => benchmarkCases
                .SelectMany(benchmarkCase => benchmarkCase.Parameters.Items)
                .Where(parameter => parameter.Value is IDisposable or IAsyncDisposable);

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
