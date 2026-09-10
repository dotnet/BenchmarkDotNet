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
    /// Each request collects into a <see cref="RequestScope"/> of its own, so that requests the platform chooses to
    /// overlap cannot take each other's values down; only completing a request touches what is held.
    /// </para>
    /// <para>
    /// The platform builds a <see cref="BenchmarkTestFramework"/> per request but this extension only once, which is
    /// why the values live here.
    /// </para>
    /// </remarks>
    internal sealed class ParameterValueLifetime : ITestHostApplicationLifetime
    {
        private readonly BenchmarkDotNetExtension extension = new();

        private readonly object gate = new();

        // Keyed by the value rather than by the ParameterInstance, because BenchmarkConverter hands the same value to
        // every job and every argument set of a benchmark, and it is to be disposed once.
        //
        // What the last completed request enumerated, and which of those BenchmarkDotNet has disposed itself because
        // it ran them.
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
        /// Starts collecting the parameter values of one request.
        /// </summary>
        /// <returns>The scope to record that request's values in, and to complete when it is over.</returns>
        public RequestScope BeginRequest() => new RequestScope(this);

        /// <inheritdoc />
        public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public async Task AfterRunAsync(int exitCode, CancellationToken cancellationToken)
        {
            List<ParameterInstance> unused;

            lock (gate)
            {
                unused = held
                    .Where(pair => !disposedByBenchmarkDotNet.Contains(pair.Key))
                    .Select(pair => pair.Value)
                    .ToList();

                held.Clear();
                disposedByBenchmarkDotNet.Clear();
            }

            await unused.DisposeAllAsync().ConfigureAwait(false);
        }

        private async ValueTask CompleteAsync(RequestScope request, IEnumerable<BenchmarkCase> ranCases)
        {
            List<ParameterInstance> gone = [];

            lock (gate)
            {
                var enumerated = request.Enumerated;

                if (!request.HasEnumerated)
                {
                    // The request never reached the end of its enumeration - the assembly failed to load, a source
                    // threw partway - so its absences say nothing about what a source would hand back, and what is
                    // held has to stay. Whatever it did manage to create joins it, rather than being lost.
                    foreach (var pair in enumerated)
                        held[pair.Key] = pair.Value;

                    return;
                }

                // Recorded before anything is chosen for disposal, so that a value BenchmarkDotNet has just disposed
                // can never also be a candidate here.
                foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(ranCases))
                    disposedByBenchmarkDotNet.Add(parameter.Value!);

                // A value the last completed request enumerated and this one did not comes from a source that
                // constructs per read: no request can hand it back again, so it goes now rather than at exit. A value
                // that came back is cached, and stays until nothing can ask for it.
                gone = held
                    .Where(pair => !enumerated.ContainsKey(pair.Key) && !disposedByBenchmarkDotNet.Contains(pair.Key))
                    .Select(pair => pair.Value)
                    .ToList();

                // Only the values that keep coming back need remembering as already disposed; a fresh one that was
                // run is gone with its request.
                disposedByBenchmarkDotNet.RemoveWhere(value => !enumerated.ContainsKey(value));

                held = enumerated;
            }

            await gone.DisposeAllAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// The parameter values one request enumerated.
        /// </summary>
        internal sealed class RequestScope
        {
            private readonly ParameterValueLifetime owner;

            internal RequestScope(ParameterValueLifetime owner) => this.owner = owner;

            /// <summary>
            /// Gets the values this request enumerated, keyed by the value itself.
            /// </summary>
            internal Dictionary<object, ParameterInstance> Enumerated { get; } = new(ParameterValueDisposer.ByReference);

            /// <summary>
            /// Gets whether the request got as far as enumerating the assembly. It tells "this request enumerated
            /// nothing" apart from "this request never got to enumerate", which is the difference between concluding
            /// that a source no longer hands a value back and having asked it nothing at all.
            /// </summary>
            internal bool HasEnumerated { get; private set; }

            /// <summary>
            /// Records the values of the benchmarks the enumeration returned, and marks the enumeration as reached.
            /// </summary>
            /// <param name="enumeratedCases">The benchmarks the enumeration returned.</param>
            public void Track(IEnumerable<BenchmarkCase> enumeratedCases)
            {
                lock (owner.gate)
                {
                    foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(enumeratedCases))
                        Enumerated[parameter.Value!] = parameter;

                    HasEnumerated = true;
                }
            }

            /// <summary>
            /// Records the values of the benchmarks that the enumeration hid, which no request will ever be handed.
            /// </summary>
            /// <remarks>
            /// Called from inside the enumeration, which may still throw afterwards, so it deliberately does not mark
            /// the enumeration as reached: being kept by the enumeration is not the same as being run, and the kept
            /// values are recorded by <see cref="Track"/> once the enumeration has returned.
            /// </remarks>
            /// <param name="enumeratedCases">Everything the assembly declares.</param>
            /// <param name="keptCases">The benchmarks the enumeration returned.</param>
            public void TrackHidden(IEnumerable<BenchmarkCase> enumeratedCases, IEnumerable<BenchmarkCase> keptCases)
            {
                lock (owner.gate)
                {
                    var kept = new HashSet<object>(
                        ParameterValueDisposer.GetDisposableParameters(keptCases).Select(parameter => parameter.Value!),
                        ParameterValueDisposer.ByReference);

                    foreach (var parameter in ParameterValueDisposer.GetDisposableParameters(enumeratedCases))
                    {
                        if (!kept.Contains(parameter.Value!))
                            Enumerated[parameter.Value!] = parameter;
                    }
                }
            }

            /// <summary>
            /// Completes the request: disposes the values of the last completed request that this one did not
            /// enumerate again, and keeps the rest for the next one.
            /// </summary>
            /// <param name="ranCases">
            /// The benchmarks whose values BenchmarkDotNet disposed itself, which it does once its run stage began -
            /// and not at all when it bailed out before that, on a critical validation error.
            /// </param>
            public ValueTask CompleteAsync(IEnumerable<BenchmarkCase> ranCases) => owner.CompleteAsync(this, ranCases);
        }
    }
}
