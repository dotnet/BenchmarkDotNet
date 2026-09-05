using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// Discovers and executes the benchmarks of the running assembly through Microsoft.Testing.Platform.
    /// </summary>
    internal sealed class BenchmarkTestFramework : ITestFramework, IDataProducer, IOutputDeviceDataProducer
    {
        private readonly BenchmarkDotNetExtension extension = new();
        private readonly IServiceProvider serviceProvider;
        private readonly Assembly assembly;

        public BenchmarkTestFramework(ITestFrameworkCapabilities capabilities, IServiceProvider serviceProvider, Assembly assembly)
        {
            Capabilities = capabilities;
            this.serviceProvider = serviceProvider;
            this.assembly = assembly;
        }

        /// <inheritdoc />
        public string Uid => extension.Uid;

        /// <inheritdoc />
        public string Version => extension.Version;

        /// <inheritdoc />
        public string DisplayName => extension.DisplayName;

        /// <inheritdoc />
        public string Description => extension.Description;

        /// <inheritdoc />
        public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

        /// <summary>
        /// Gets the capabilities the framework was registered with.
        /// </summary>
        public ITestFrameworkCapabilities Capabilities { get; }

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync() => extension.IsEnabledAsync();

        /// <inheritdoc />
        public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
            => Task.FromResult(new CreateTestSessionResult { IsSuccess = true });

        /// <inheritdoc />
        public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
            => Task.FromResult(new CloseTestSessionResult { IsSuccess = true });

        /// <inheritdoc />
        public async Task ExecuteRequestAsync(ExecuteRequestContext context)
        {
            try
            {
                switch (context.Request)
                {
                    case DiscoverTestExecutionRequest discoverRequest:
                        await DiscoverAsync(discoverRequest, context).ConfigureAwait(false);
                        break;
                    case RunTestExecutionRequest runRequest:
                        await RunAsync(runRequest, context).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                context.Complete();
            }
        }

        private async Task DiscoverAsync(DiscoverTestExecutionRequest request, ExecuteRequestContext context)
        {
            var enumeration = GetMatchingBenchmarks(request.Filter);

            try
            {
                foreach (var benchmarks in enumeration.Matches)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    // Exactly one node per uid: publishing a colliding uid twice would leave the platform with two
                    // nodes it cannot tell apart. The collision itself is reported when the benchmarks are run.
                    var message = new TestNodeUpdateMessage(
                        request.Session.SessionUid,
                        benchmarks[0].Node.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance));

                    await context.MessageBus.PublishAsync(this, message).ConfigureAwait(false);
                }
            }
            finally
            {
                // Discovery runs nothing, so every value the enumeration created is this method's to dispose.
                DisposeUnusedParameterValues(enumeration.All, []);
            }
        }

        private async Task RunAsync(RunTestExecutionRequest request, ExecuteRequestContext context)
        {
            var sessionUid = request.Session.SessionUid;
            var cancellationToken = context.CancellationToken;

            var runnable = new List<Match>();
            var enumeration = GetMatchingBenchmarks(request.Filter);
            foreach (var benchmarks in enumeration.Matches)
            {
                if (benchmarks.Count == 1)
                    runnable.Add(benchmarks[0]);
                else
                    await PublishCollisionAsync(context, sessionUid, benchmarks).ConfigureAwait(false);
            }

            // A benchmark that was filtered out or that collided is never handed to BenchmarkDotNet, so nothing else
            // would dispose the values the enumeration created for it.
            DisposeUnusedParameterValues(enumeration.All, runnable.Select(match => match.Node.BenchmarkCase));

            if (runnable.Count == 0)
                return;

            var nodes = runnable.ToDictionary(match => match.Node.Uid, match => match.Node);

            // BenchmarkDotNet reports its progress through synchronous callbacks (EventProcessor and ILogger) while
            // the message bus and the output device are asynchronous. Blocking on those from inside a callback risks
            // deadlocking against the synchronization context BenchmarkDotNet installs while it runs, so the callbacks
            // write to this channel and the drain below does the awaiting. Synchronous continuations are left off, so
            // that a write can never end up publishing on BenchmarkDotNet's own thread.
            var workQueue = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

            // A failure while publishing has to stop the benchmarks as well, otherwise the run would carry on with
            // nobody listening to it.
            using var runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var eventProcessor = new BenchmarkEventProcessor(nodes, testNode =>
            {
                var message = new TestNodeUpdateMessage(sessionUid, testNode);
                workQueue.Writer.TryWrite(() => context.MessageBus.PublishAsync(this, message));
            });

            // BenchmarkDotNet's own console output is replaced so that everything goes through the output device,
            // which keeps it in the right place when the platform runs in server mode or inside an IDE.
            var logger = new OutputDeviceLogger(serviceProvider.GetOutputDevice(), this, workQueue.Writer, cancellationToken);

            var runInfos = runnable
                .GroupBy(match => match.RunInfo)
                .Select(group => new BenchmarkRunInfo(
                    group.Select(match => match.Node.BenchmarkCase).ToArray(),
                    group.Key.Type,
                    group.Key.Config
                        .AddEventProcessor(eventProcessor)
                        .AddLogger(logger)
                        .RemoveLoggersOfType<ConsoleLogger>()
                        .CreateImmutableConfig(),
                    group.Key.CompositeInProcessDiagnoser))
                .ToArray();

            // BenchmarkDotNet blocks the calling thread for the whole run, so it gets a thread of its own and the
            // queued messages are published from here as they are produced.
            var runTask = Task.Run(
                () =>
                {
                    try
                    {
                        BenchmarkRunner.Run(runInfos, runCancellation.Token);
                    }
                    finally
                    {
                        try
                        {
                            // Benchmarks that never reported a result still need one, unless the run was cancelled.
                            // Two things make the cancelled run the exception:
                            //
                            //  * The platform's contract for a cancelled request is an OperationCanceledException,
                            //    not a terminal state per node. CancelledTestNodeStateProperty is obsolete for
                            //    exactly this reason, so a node left in progress is the shape it asks for.
                            //    BenchmarkDotNet rethrows the cancellation, and awaiting the run task below surfaces
                            //    it out of the request.
                            //  * The token is also cancelled when publishing itself failed. Nothing drains the queue
                            //    at that point, so results published here would be dropped anyway.
                            if (!runCancellation.IsCancellationRequested)
                                eventProcessor.PublishOutstandingResults();

                            logger.Flush();
                        }
                        finally
                        {
                            // The drain only ends once the queue is completed, so this has to happen no matter what
                            // else went wrong.
                            workQueue.Writer.TryComplete();
                        }
                    }
                },
                CancellationToken.None);

            ExceptionDispatchInfo? drainFailure = null;
            try
            {
                await DrainAsync(workQueue.Reader).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // Nothing consumes the queue anymore, so the run has to be stopped rather than left orphaned. This is
                // also the path a cancelled run takes, since the queued writes are handed the platform's token.
                drainFailure = ExceptionDispatchInfo.Capture(exception);
                runCancellation.Cancel();
            }

            try
            {
                // The run has to be over before the request completes, otherwise it would carry on in the background
                // and its failure would go unobserved.
                await runTask.ConfigureAwait(false);
            }
            catch when (drainFailure != null)
            {
                // The run was stopped because publishing failed, so that failure is the one worth reporting.
            }

            drainFailure?.Throw();
        }

        /// <summary>
        /// Runs the queued work items in order, until the queue is completed and empty.
        /// </summary>
        /// <param name="reader">The reader of the work queue.</param>
        private static async Task DrainAsync(ChannelReader<Func<Task>> reader)
        {
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var work))
                    await work().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reports benchmarks that share a uid as a single failed test.
        /// </summary>
        /// <remarks>
        /// The platform identifies test nodes by uid, so benchmarks that produce the same one cannot be reported
        /// separately. Failing them keeps the rest of the run going, which is more useful than aborting the request.
        /// </remarks>
        private async Task PublishCollisionAsync(ExecuteRequestContext context, SessionUid sessionUid, List<Match> collision)
        {
            var node = collision[0].Node;

            // The colliding benchmarks share every string the uid is built from, so the method names are the only
            // thing left that can tell them apart. They are the same name when it is the parameters that collide.
            var methodNames = string.Join(", ", collision
                .Select(match => match.Node.BenchmarkCase.Descriptor.WorkloadMethod.Name)
                .Distinct(StringComparer.Ordinal));
            var error =
                $"{collision.Count} benchmarks are identified as '{node.Uid}' and cannot be told apart, so none of " +
                $"them were run: {methodNames}. The identity is built from the type, the benchmark name " +
                "([Benchmark(Description = \"...\")] when set, the method name otherwise), the job, and the string " +
                "representation of the parameters. Give the colliding benchmarks distinct descriptions, distinct " +
                "jobs, or distinct parameter ToString() results.";

            await context.MessageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(sessionUid, node.ToTestNode(InProgressTestNodeStateProperty.CachedInstance))).ConfigureAwait(false);

            await context.MessageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(sessionUid, node.ToTestNode(new FailedTestNodeStateProperty(error)))).ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates the benchmarks of the assembly and keeps the ones the request asked for.
        /// </summary>
        /// <param name="filter">The filter of the request.</param>
        /// <returns>
        /// The matching benchmarks in enumeration order and grouped by uid, together with everything the assembly
        /// declares. A group holding more than one benchmark is a uid collision.
        /// </returns>
        private Enumeration GetMatchingBenchmarks(ITestExecutionFilter filter)
        {
            var matches = new List<List<Match>>();
            var matchesByUid = new Dictionary<string, List<Match>>(StringComparer.Ordinal);
            var runInfos = BenchmarkEnumerator.GetBenchmarksFromAssembly(assembly);

            foreach (var runInfo in runInfos)
            {
                // The job only earns a place in the display name when the benchmark actually runs under several jobs.
                // This is computed before filtering so that a benchmark keeps the same name however it was selected.
                var includeJobInName = runInfo.BenchmarksCases.Select(c => c.Job.DisplayInfo).Distinct().Count() > 1;

                foreach (var benchmarkCase in runInfo.BenchmarksCases)
                {
                    var node = BenchmarkTestNode.Create(benchmarkCase, includeJobInName);
                    if (!Matches(filter, node))
                        continue;

                    if (!matchesByUid.TryGetValue(node.Uid, out var sameUid))
                    {
                        sameUid = new List<Match>();
                        matchesByUid.Add(node.Uid, sameUid);
                        matches.Add(sameUid);
                    }

                    sameUid.Add(new Match(runInfo, node));
                }
            }

            return new Enumeration(matches, runInfos);
        }

        /// <summary>
        /// Disposes the parameter values of the benchmarks that were enumerated but will not be run.
        /// </summary>
        /// <remarks>
        /// Enumerating an assembly instantiates the values of every [Params] and [ArgumentsSource], and BenchmarkDotNet
        /// only disposes the ones belonging to the benchmarks it was handed. The values are matched by reference
        /// instead of being disposed case by case, because BenchmarkConverter gives the same ParameterInstance to
        /// every job and every argument set of a benchmark: disposing a filtered out case wholesale would take down
        /// values that a benchmark which is about to run still owns.
        /// </remarks>
        /// <param name="enumerated">Everything the assembly declares.</param>
        /// <param name="retained">The benchmarks that are going to be run, if any.</param>
        private static void DisposeUnusedParameterValues(BenchmarkRunInfo[] enumerated, IEnumerable<BenchmarkCase> retained)
        {
            var unused = new HashSet<IDisposable>(ReferenceComparer.Instance);

            foreach (var value in GetDisposableParameterValues(enumerated.SelectMany(runInfo => runInfo.BenchmarksCases)))
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

#pragma warning disable TPEXP // The tree node filter is still marked as experimental by the platform.
        private static bool Matches(ITestExecutionFilter filter, BenchmarkTestNode node) => filter switch
        {
            TestNodeUidListFilter uidListFilter => uidListFilter.TestNodeUids.Any(uid => uid.Value == node.Uid),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(node.Path, node.GetFilterableProperties()),

            // NopFilter, and anything the platform adds later, means "everything".
            _ => true
        };
#pragma warning restore TPEXP

        /// <summary>
        /// The result of enumerating the assembly for a request.
        /// </summary>
        private sealed class Enumeration
        {
            public Enumeration(List<List<Match>> matches, BenchmarkRunInfo[] all)
            {
                Matches = matches;
                All = all;
            }

            /// <summary>
            /// Gets the benchmarks the request asked for, grouped by uid.
            /// </summary>
            public List<List<Match>> Matches { get; }

            /// <summary>
            /// Gets every benchmark the assembly declares, matching or not.
            /// </summary>
            public BenchmarkRunInfo[] All { get; }
        }

        /// <summary>
        /// Compares by reference, so that a parameter value which overrides Equals is still disposed once per instance.
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<IDisposable>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(IDisposable? x, IDisposable? y) => ReferenceEquals(x, y);

            public int GetHashCode(IDisposable obj) => RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>
        /// A benchmark that matched the request, together with the run info it belongs to.
        /// </summary>
        private sealed class Match
        {
            public Match(BenchmarkRunInfo runInfo, BenchmarkTestNode node)
            {
                RunInfo = runInfo;
                Node = node;
            }

            public BenchmarkRunInfo RunInfo { get; }

            public BenchmarkTestNode Node { get; }
        }
    }
}
