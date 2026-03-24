using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.EventProcessors;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class CallerThreadTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetToolchains() =>
    [
        new InProcessEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        new InProcessNoEmitToolchain(new() { ExecuteOnSeparateThread = false }),
        Job.Default.GetToolchain()
    ];

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void UserCodeRunsOnCallerThread(IToolchain toolchain)
    {
        var threadTracker = new ThreadTracker();

        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain))
            .AddExporter(threadTracker.Exporter)
            .AddValidator(threadTracker.Validator)
            .AddAnalyser(threadTracker.Analyser)
            .AddDiagnoser(threadTracker.Diagnoser)
            .AddEventProcessor(threadTracker.EventProcessor)
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

        using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
        threadTracker.CallerThreadId = Thread.CurrentThread.ManagedThreadId;
        var valueTask = CanExecuteAsync<CallerThreadSimpleBenchmark>(config);
        context.ExecuteUntilComplete(valueTask);

        Assert.NotEqual(0, threadTracker.CallerThreadId);

        foreach (var (source, threadId) in threadTracker.RecordedThreadIds)
        {
            Assert.True(threadTracker.CallerThreadId == threadId,
                $"{source} ran on thread {threadId}, expected caller thread {threadTracker.CallerThreadId}");
        }
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Exporter.ExportAsync");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Validator.ValidateAsync");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Analyser.Analyse");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Diagnoser.ValidateAsync");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Diagnoser.DisplayResults");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source == "Diagnoser.ProcessResults");
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source.StartsWith("Diagnoser.HandleAsync("));
        Assert.Contains(threadTracker.RecordedThreadIds, r => r.Source.StartsWith("EventProcessor."));
    }

    private class ThreadTracker
    {
        public int CallerThreadId { get; set; }
        public List<(string Source, int ThreadId)> RecordedThreadIds { get; } = [];

        public ThreadTrackingExporter Exporter { get; }
        public ThreadTrackingValidator Validator { get; }
        public ThreadTrackingAnalyser Analyser { get; }
        public ThreadTrackingDiagnoser Diagnoser { get; }
        public ThreadTrackingEventProcessor EventProcessor { get; }

        public ThreadTracker()
        {
            Exporter = new ThreadTrackingExporter(this);
            Validator = new ThreadTrackingValidator(this);
            Analyser = new ThreadTrackingAnalyser(this);
            Diagnoser = new ThreadTrackingDiagnoser(this);
            EventProcessor = new ThreadTrackingEventProcessor(this);
        }

        public void Record(string source)
        {
            lock (RecordedThreadIds)
            {
                RecordedThreadIds.Add((source, Thread.CurrentThread.ManagedThreadId));
            }
        }
    }

    private class ThreadTrackingExporter(ThreadTracker tracker) : IExporter
    {
        public string Name => nameof(ThreadTrackingExporter);

        public ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
            tracker.Record("Exporter.ExportAsync");
            return new();
        }
    }

    private class ThreadTrackingValidator(ThreadTracker tracker) : IValidator
    {
        public bool TreatsWarningsAsErrors => false;

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            tracker.Record("Validator.ValidateAsync");
            await Task.CompletedTask;
            yield break;
        }
    }

    private class ThreadTrackingAnalyser(ThreadTracker tracker) : IAnalyser
    {
        public string Id => nameof(ThreadTrackingAnalyser);

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            tracker.Record("Analyser.Analyse");
            return [];
        }
    }

    private class ThreadTrackingDiagnoser(ThreadTracker tracker) : IDiagnoser
    {
        public IEnumerable<string> Ids => [nameof(ThreadTrackingDiagnoser)];
        public IEnumerable<IExporter> Exporters => [];
        public IEnumerable<IAnalyser> Analysers => [];
        public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase) => BenchmarkDotNet.Diagnosers.RunMode.NoOverhead;

        public ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            tracker.Record($"Diagnoser.HandleAsync({signal})");
            return new();
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            tracker.Record("Diagnoser.ProcessResults");
            return [];
        }

        public void DisplayResults(ILogger logger)
        {
            tracker.Record("Diagnoser.DisplayResults");
        }

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            tracker.Record("Diagnoser.ValidateAsync");
            yield break;
        }
    }

    private class ThreadTrackingEventProcessor(ThreadTracker tracker) : EventProcessor
    {
        public override void OnStartValidationStage() => tracker.Record("EventProcessor.OnStartValidationStage");
        public override void OnEndValidationStage() => tracker.Record("EventProcessor.OnEndValidationStage");
        public override void OnStartBuildStage(IReadOnlyList<BuildPartition> partitions) => tracker.Record("EventProcessor.OnStartBuildStage");
        public override void OnBuildComplete(BuildPartition partition, BuildResult buildResult) => tracker.Record("EventProcessor.OnBuildComplete");
        public override void OnEndBuildStage() => tracker.Record("EventProcessor.OnEndBuildStage");
        public override void OnStartRunStage() => tracker.Record("EventProcessor.OnStartRunStage");
        public override void OnStartRunBenchmarksInType(System.Type type, IReadOnlyList<BenchmarkCase> benchmarks) => tracker.Record("EventProcessor.OnStartRunBenchmarksInType");
        public override void OnEndRunBenchmarksInType(System.Type type, Summary summary) => tracker.Record("EventProcessor.OnEndRunBenchmarksInType");
        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase) => tracker.Record("EventProcessor.OnStartRunBenchmark");
        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report) => tracker.Record("EventProcessor.OnEndRunBenchmark");
        public override void OnEndRunStage() => tracker.Record("EventProcessor.OnEndRunStage");
    }

    public class CallerThreadSimpleBenchmark
    {
        [Benchmark]
        public void Run() => Thread.Sleep(1);
    }
}
