using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using Microsoft.Testing.Platform.Extensions.TestHost;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// Owns the parameter values that the requests of a test application enumerate and do not run, and disposes them
    /// once no request can hand them back again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposing a value at the end of the request that enumerated it is wrong under server mode, which is how Visual
    /// Studio and the VS Code Test Explorer drive the platform: one process serves a discovery request and the run
    /// requests that follow it, every request enumerates the assembly again, and a [ParamsSource] backed by a cached
    /// collection - a static field, a property over a readonly array - hands back the very same objects. Discovery
    /// would dispose the values the run is about to execute against.
    /// </para>
    /// <para>
    /// Holding every value until the application ends is wrong the other way round: a source that constructs per
    /// read - <c>yield return new FileStream(...)</c>, the common shape - produces fresh objects on every request,
    /// none of which a later request can reuse, and a long session would pile them up. The two are told apart by
    /// what the next request enumerates: a value that comes back is cached and stays, a value that does not is gone
    /// for good and is disposed then. The same rule bounds what is remembered about the values BenchmarkDotNet
    /// disposed itself.
    /// </para>
    /// <para>
    /// The platform builds a <see cref="BenchmarkTestFramework"/> per request but this extension only once, which is
    /// why the values live here.
    /// </para>
    /// </remarks>
    internal sealed class ParameterValueLifetime : ITestHostApplicationLifetime
    {
        private readonly BenchmarkDotNetExtension extension = new();

        // Requests are served one at a time, but the platform is free to change that, and getting this wrong would
        // leak or double dispose rather than fail visibly.
        private readonly object gate = new();

        // Keyed by the value rather than by the ParameterInstance, because BenchmarkConverter hands the same value
        // to every job and every argument set of a benchmark, and it is to be disposed once.
        //
        // What the request in flight has enumerated so far; what the last completed request enumerated, and which
        // of those BenchmarkDotNet has disposed itself because it ran them.
        private Dictionary<object, ParameterInstance> inFlight = new(ParameterValueDisposer.ByReference);
        private Dictionary<object, ParameterInstance> held = new(ParameterValueDisposer.ByReference);
        private readonly HashSet<object> disposedByBenchmarkDotNet = new(ParameterValueDisposer.ByReference);

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
        /// Records the values the request in flight enumerated. Nothing is disposed until the request completes.
        /// </summary>
        /// <param name="enumeratedCases">The benchmarks the request enumerated.</param>
        public void Track(IEnumerable<BenchmarkCase> enumeratedCases)
        {
            lock (gate)
            {
                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(enumeratedCases))
                    inFlight[parameter.Value!] = parameter;
            }
        }

        /// <summary>
        /// Records the values of the benchmarks that the enumeration hid, which no request will ever be handed.
        /// </summary>
        /// <remarks>
        /// Being kept by the enumeration is not the same as being run, so the kept values are only excluded from what
        /// is collected here: they are recorded through <see cref="Track"/> once the enumeration has returned.
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
                        inFlight[parameter.Value!] = parameter;
                }
            }
        }

        /// <summary>
        /// Completes the request in flight: disposes the values of the previous request that this one did not
        /// enumerate again, and keeps the rest for the next one.
        /// </summary>
        /// <param name="ranCases">
        /// The benchmarks whose values BenchmarkDotNet disposed itself, which it does once its run stage began - and
        /// not at all when it bailed out before that, on a critical validation error.
        /// </param>
        public async ValueTask CompleteRequestAsync(IEnumerable<BenchmarkCase> ranCases)
        {
            List<ParameterInstance> gone;

            lock (gate)
            {
                var current = inFlight;
                inFlight = new Dictionary<object, ParameterInstance>(ParameterValueDisposer.ByReference);

                // A value the previous request enumerated and this one did not comes from a source that constructs
                // per read: no request can hand it back again, so it goes now rather than at exit. A value that came
                // back is cached, and stays until nothing can ask for it.
                gone = held
                    .Where(pair => !current.ContainsKey(pair.Key) && !disposedByBenchmarkDotNet.Contains(pair.Key))
                    .Select(pair => pair.Value)
                    .ToList();

                // Only the values that keep coming back need remembering as already disposed; a fresh one that was
                // run is gone with its request.
                disposedByBenchmarkDotNet.RemoveWhere(value => !current.ContainsKey(value));
                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(ranCases))
                    disposedByBenchmarkDotNet.Add(parameter.Value!);

                held = current;
            }

            await gone.DisposeAllAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public async Task AfterRunAsync(int exitCode, CancellationToken cancellationToken)
        {
            List<ParameterInstance> unused;

            lock (gate)
            {
                // Every request completes through CompleteRequestAsync, so nothing should be in flight here, but a
                // value that somehow is would otherwise be leaked for good.
                unused = held.Concat(inFlight)
                    .Where(pair => !disposedByBenchmarkDotNet.Contains(pair.Key))
                    .GroupBy(pair => pair.Key, ParameterValueDisposer.ByReference)
                    .Select(group => group.First().Value)
                    .ToList();

                held.Clear();
                inFlight.Clear();
                disposedByBenchmarkDotNet.Clear();
            }

            await unused.DisposeAllAsync().ConfigureAwait(false);
        }
    }
}
