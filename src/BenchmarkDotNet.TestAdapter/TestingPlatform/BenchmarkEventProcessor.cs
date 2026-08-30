using BenchmarkDotNet.EventProcessors;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Microsoft.Testing.Platform.Extensions.Messages;
using Perfolizer.Mathematics.Histograms;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{

    /// <summary>
    /// Translates the BenchmarkDotNet run into the stream of test node updates the platform expects.
    /// </summary>
    internal sealed class BenchmarkEventProcessor : EventProcessor
    {
        private readonly IReadOnlyDictionary<string, BenchmarkTestNode> nodes;
        private readonly Action<TestNode> publish;
        private readonly Stopwatch runTimerStopwatch = new();
        private readonly Dictionary<string, PendingResult> pendingResults = [];
        private readonly HashSet<string> publishedResults = [];

        // BenchmarkDotNet builds the partitions in parallel and raises OnBuildComplete from each of the build tasks,
        // so that callback can run on several threads at once. The others cannot: every build is awaited before the
        // first benchmark runs, and the benchmarks themselves run one after another.
        private readonly object buildCompleteGate = new();

        public BenchmarkEventProcessor(IReadOnlyDictionary<string, BenchmarkTestNode> nodes, Action<TestNode> publish)
        {
            this.nodes = nodes;
            this.publish = publish;
        }

        public override void OnValidationError(ValidationError validationError)
        {
            // If the error is not linked to a benchmark case, then set the error on all benchmarks.
            var affected = validationError.BenchmarkCase == null
                ? nodes.Values
                : [nodes[validationError.BenchmarkCase.GetUniqueId()]];

            foreach (var node in affected)
            {
                var pending = GetOrCreatePendingResult(node);

                if (validationError.IsCritical)
                {
                    // The result is not published yet, in case there are more validation errors to append.
                    pending.ErrorMessages.Add(validationError.Message);
                }
                else
                {
                    pending.Output.AppendLine($"WARNING: {validationError.Message}");
                }
            }
        }

        public override void OnBuildComplete(BuildPartition buildPartition, BuildResult buildResult)
        {
            // Only build failures need to be reported, successful builds are followed by a run.
            if (buildResult.IsBuildSuccess)
                return;

            lock (buildCompleteGate)
            {
                foreach (var benchmarkBuildInfo in buildPartition.Benchmarks)
                {
                    var node = nodes[benchmarkBuildInfo.BenchmarkCase.GetUniqueId()];
                    var pending = GetOrCreatePendingResult(node);

                    if (buildResult.GenerateException != null)
                        pending.ErrorMessages.Add($"// Generate Exception: {buildResult.GenerateException.Message}");
                    else if (buildResult.TryToExplainFailureReason(buildPartition.GetInProcessDiagnoserHandlerTypes(), out string? reason))
                        pending.ErrorMessages.Add($"// Build Error: {reason}");
                    else if (buildResult.ErrorMessage != null)
                        pending.ErrorMessages.Add($"// Build Error: {buildResult.ErrorMessage}");

                    // A benchmark that failed to build will never run, so the result can be published immediately.
                    publish(node.ToTestNode(InProgressTestNodeStateProperty.CachedInstance));
                    PublishResult(node, pending, new FailedTestNodeStateProperty(pending.GetErrorMessage() ?? "The benchmark failed to build."));
                }
            }
        }

        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
        {
            var node = nodes[benchmarkCase.GetUniqueId()];
            var pending = GetOrCreatePendingResult(node);
            pending.StartTime = DateTimeOffset.UtcNow;

            publish(node.ToTestNode(InProgressTestNodeStateProperty.CachedInstance));
            runTimerStopwatch.Restart();
        }

        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            var node = nodes[benchmarkCase.GetUniqueId()];
            var pending = GetOrCreatePendingResult(node);
            pending.Duration = runTimerStopwatch.Elapsed;
            pending.EndTime = DateTimeOffset.UtcNow;

            AppendMeasurementSummary(pending.Output, report);

            TestNodeStateProperty state = report.Success && pending.ErrorMessages.Count == 0
                ? PassedTestNodeStateProperty.CachedInstance
                : new FailedTestNodeStateProperty(pending.GetErrorMessage() ?? "The benchmark did not complete successfully.");

            PublishResult(node, pending, state);
        }

        /// <summary>
        /// Publishes a result for every benchmark that was scheduled to run but never reported one, which happens when
        /// a critical validation error stopped it or when BenchmarkDotNet never reached it.
        /// </summary>
        public void PublishOutstandingResults()
        {
            foreach (var node in nodes.Values)
            {
                if (publishedResults.Contains(node.Uid))
                    continue;

                var pending = GetOrCreatePendingResult(node);
                var errorMessage = pending.GetErrorMessage();

                TestNodeStateProperty state = errorMessage != null
                    ? new FailedTestNodeStateProperty(errorMessage)
                    : SkippedTestNodeStateProperty.CachedInstance;

                publish(node.ToTestNode(InProgressTestNodeStateProperty.CachedInstance));
                PublishResult(node, pending, state);
            }
        }

        private void PublishResult(BenchmarkTestNode node, PendingResult pending, TestNodeStateProperty state)
        {
            var properties = new List<IProperty>();

            if (pending.StartTime is { } startTime)
            {
                var duration = pending.Duration ?? TimeSpan.Zero;
                properties.Add(new TimingProperty(new TimingInfo(startTime, pending.EndTime ?? startTime + duration, duration)));
            }

            if (pending.Output.Length > 0)
                properties.Add(new StandardOutputProperty(pending.Output.ToString()));

            publish(node.ToTestNode(state, properties.ToArray()));
            publishedResults.Add(node.Uid);
        }

        private PendingResult GetOrCreatePendingResult(BenchmarkTestNode node)
        {
            if (!pendingResults.TryGetValue(node.Uid, out var pending))
            {
                pending = new PendingResult();
                pendingResults[node.Uid] = pending;
            }

            return pending;
        }

        private static void AppendMeasurementSummary(StringBuilder output, BenchmarkReport report)
        {
            var resultRuns = report.GetResultRuns();
            if (resultRuns.Count == 0)
                return;

            output.AppendLine(report.BenchmarkCase.DisplayInfo);
            output.AppendLine($"Runtime = {report.GetRuntimeInfo()}; GC = {report.GetGcInfo()}");

            var statistics = resultRuns.GetStatistics();
            var cultureInfo = CultureInfo.InvariantCulture;
            var formatter = statistics.CreateNanosecondFormatter(cultureInfo);

            var histogram = HistogramBuilder.Adaptive.Build(statistics.Sample.Values);
            output.AppendLine("-------------------- Histogram --------------------");
            output.AppendLine(histogram.ToString(formatter));
            output.AppendLine("---------------------------------------------------");
            output.AppendLine(statistics.ToString(cultureInfo, formatter, calcHistogram: false));
        }

        private sealed class PendingResult
        {
            public List<string> ErrorMessages { get; } = [];

            public StringBuilder Output { get; } = new();

            public DateTimeOffset? StartTime { get; set; }

            public DateTimeOffset? EndTime { get; set; }

            public TimeSpan? Duration { get; set; }

            public string? GetErrorMessage() => ErrorMessages.Count == 0 ? null : string.Join("\n", ErrorMessages);
        }
    }
}
