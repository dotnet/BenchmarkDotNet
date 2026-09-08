using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using Microsoft.Testing.Platform.Extensions.TestHost;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// Holds the parameter values that the requests of a test application enumerated but never ran, and disposes them
    /// once the application is done.
    /// </summary>
    /// <remarks>
    /// Disposing them at the end of the request that enumerated them is wrong under server mode, which is how Visual
    /// Studio and the VS Code Test Explorer drive the platform: a discovery request and the run requests that follow
    /// it are served by the same process, every request enumerates the assembly again, and a [ParamsSource] backed by
    /// a cached collection - a static field, or a property over a readonly array - hands back the very same objects.
    /// Discovery would then dispose the values the run is about to execute against. The platform builds a
    /// <see cref="BenchmarkTestFramework"/> per request but this extension only once, so it is what can hold the
    /// values until nothing can ask for them again.
    /// </remarks>
    internal sealed class ParameterValueLifetime : ITestHostApplicationLifetime
    {
        private readonly BenchmarkDotNetExtension extension = new();

        // Requests are served one at a time, but the platform is free to change that, and getting this wrong would
        // leak or double dispose rather than fail visibly.
        private readonly object gate = new();

        // Everything the requests enumerated, keyed by the value so that a ParameterInstance which BenchmarkConverter
        // handed to several jobs is only disposed once, and everything that was handed to BenchmarkDotNet, which
        // disposes what it was given itself. Retention is remembered for the whole application: a value that one
        // request ran is disposed by BenchmarkDotNet, however many later requests enumerate it again.
        private readonly Dictionary<object, ParameterInstance> enumerated = new(ParameterValueDisposer.ByReference);
        private readonly HashSet<object> retained = new(ParameterValueDisposer.ByReference);

        /// <inheritdoc />
        public string Uid => extension.Uid + ".ParameterValueLifetime";

        /// <inheritdoc />
        public string Version => extension.Version;

        /// <inheritdoc />
        public string DisplayName => extension.DisplayName;

        /// <inheritdoc />
        public string Description => extension.Description;

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync() => extension.IsEnabledAsync();

        /// <summary>
        /// Records what a request enumerated and what of it was handed to BenchmarkDotNet, without disposing
        /// anything yet.
        /// </summary>
        /// <param name="enumeratedCases">Everything the request enumerated.</param>
        /// <param name="retainedCases">The benchmarks that were handed to BenchmarkDotNet, if any.</param>
        public void Track(IEnumerable<BenchmarkCase> enumeratedCases, IEnumerable<BenchmarkCase> retainedCases)
        {
            lock (gate)
            {
                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(enumeratedCases))
                    enumerated[parameter.Value!] = parameter;

                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(retainedCases))
                    retained.Add(parameter.Value!);
            }
        }

        /// <summary>
        /// Records the values of the benchmarks that the enumeration hid, which no request will ever be handed.
        /// </summary>
        /// <remarks>
        /// Being kept by the enumeration is not the same as being handed to BenchmarkDotNet, so the kept values are
        /// only excluded from what is collected here: whether they are ever run is for <see cref="Track"/> to say.
        /// </remarks>
        /// <param name="enumeratedCases">Everything the assembly declares.</param>
        /// <param name="keptCases">The benchmarks the enumeration returned.</param>
        public void TrackHidden(IEnumerable<BenchmarkCase> enumeratedCases, IEnumerable<BenchmarkCase> keptCases)
        {
            lock (gate)
            {
                var kept = new HashSet<object>(
                    ParameterValueDisposer.GetDisposableParameters(keptCases).Select(parameter => parameter.Value!),
                    ParameterValueDisposer.ByReference);

                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(enumeratedCases))
                {
                    if (!kept.Contains(parameter.Value!))
                        enumerated[parameter.Value!] = parameter;
                }
            }
        }

        /// <inheritdoc />
        public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public async Task AfterRunAsync(int exitCode, CancellationToken cancellationToken)
        {
            List<ParameterInstance> unused;

            lock (gate)
            {
                unused = enumerated.Where(pair => !retained.Contains(pair.Key)).Select(pair => pair.Value).ToList();
                enumerated.Clear();
                retained.Clear();
            }

            await unused.DisposeAllAsync().ConfigureAwait(false);
        }
    }
}
