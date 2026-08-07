using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using System.Reflection;

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
            foreach (var (_, node) in GetMatchingBenchmarks(request.Filter))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var message = new TestNodeUpdateMessage(
                    request.Session.SessionUid,
                    node.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance));

                await context.MessageBus.PublishAsync(this, message).ConfigureAwait(false);
            }
        }

        private async Task RunAsync(RunTestExecutionRequest request, ExecuteRequestContext context)
        {
            var matches = GetMatchingBenchmarks(request.Filter);
            if (matches.Count == 0)
                return;

            var nodes = matches.ToDictionary(match => match.Node.Uid, match => match.Node);
            var sessionUid = request.Session.SessionUid;
            var cancellationToken = context.CancellationToken;

            using var workQueue = new AsyncWorkQueue();

            var eventProcessor = new BenchmarkEventProcessor(nodes, testNode =>
            {
                var message = new TestNodeUpdateMessage(sessionUid, testNode);
                workQueue.Enqueue(() => context.MessageBus.PublishAsync(this, message));
            });

            // BenchmarkDotNet's own console output is replaced so that everything goes through the output device,
            // which keeps it in the right place when the platform runs in server mode or inside an IDE.
            var logger = new OutputDeviceLogger(serviceProvider.GetOutputDevice(), this, workQueue, cancellationToken);

            var runInfos = matches
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
                        BenchmarkRunner.Run(runInfos, cancellationToken);
                    }
                    finally
                    {
                        // Benchmarks that never reported a result still need one, unless the run was cancelled: the
                        // platform expects an OperationCanceledException in that case, and publishing results
                        // afterwards would contradict it.
                        if (!cancellationToken.IsCancellationRequested)
                            eventProcessor.PublishOutstandingResults();

                        logger.Flush();
                        workQueue.Complete();
                    }
                },
                CancellationToken.None);

            await workQueue.DrainAsync().ConfigureAwait(false);
            await runTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates the benchmarks of the assembly and keeps the ones the request asked for.
        /// </summary>
        /// <param name="filter">The filter of the request.</param>
        /// <returns>The matching benchmarks, paired with the run info they belong to.</returns>
        private List<(BenchmarkRunInfo RunInfo, BenchmarkTestNode Node)> GetMatchingBenchmarks(ITestExecutionFilter filter)
        {
            var matches = new List<(BenchmarkRunInfo, BenchmarkTestNode)>();

            foreach (var runInfo in BenchmarkEnumerator.GetBenchmarksFromAssembly(assembly))
            {
                // The job only earns a place in the display name when the benchmark actually runs under several jobs.
                // This is computed before filtering so that a benchmark keeps the same name however it was selected.
                var includeJobInName = runInfo.BenchmarksCases.Select(c => c.Job.DisplayInfo).Distinct().Count() > 1;

                foreach (var benchmarkCase in runInfo.BenchmarksCases)
                {
                    var node = BenchmarkTestNode.Create(benchmarkCase, includeJobInName);
                    if (Matches(filter, node))
                        matches.Add((runInfo, node));
                }
            }

            return matches;
        }

#pragma warning disable TPEXP // The tree node filter is still marked as experimental by the platform.
        private static bool Matches(ITestExecutionFilter filter, BenchmarkTestNode node) => filter switch
        {
            TestNodeUidListFilter uidListFilter => uidListFilter.TestNodeUids.Any(uid => uid.Value == node.Uid),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(node.Path, node.GetFilterableProperties()),

            // NopFilter, and anything the platform adds later, means "everything".
            _ => true
        };
#pragma warning restore TPEXP
    }
}
