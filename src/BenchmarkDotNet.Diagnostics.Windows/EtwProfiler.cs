using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Tracing;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class EtwProfiler : IDiagnoser, IHardwareCountersDiagnoser, IProfiler
    {
        private readonly EtwProfilerConfig config;
        private readonly RunMode runMode;
        private readonly Dictionary<BenchmarkCase, string> benchmarkToEtlFile;
        private readonly Dictionary<BenchmarkCase, PreciseMachineCounter[]> benchmarkToCounters;

        private Session kernelSession, userSession, heapSession;

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public EtwProfiler() : this(new EtwProfilerConfig(performExtraBenchmarksRun: false)) { }

        [PublicAPI]
        public EtwProfiler(EtwProfilerConfig config)
        {
            this.config = config;
            runMode = config.PerformExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;
            benchmarkToEtlFile = new Dictionary<BenchmarkCase, string>();
            benchmarkToCounters = new Dictionary<BenchmarkCase, PreciseMachineCounter[]>();
            CreationTime = DateTime.Now;
        }

        public string ShortName => "ETW";

        public IEnumerable<string> Ids => new[] { nameof(EtwProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public IReadOnlyDictionary<BenchmarkCase, PmcStats> Results => BuildPmcStats();

        internal IReadOnlyDictionary<BenchmarkCase, string> BenchmarkToEtlFile => benchmarkToEtlFile;

        private DateTime CreationTime { get; }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => runMode;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => HardwareCounters.Validate(validationParameters, mandatory: false);

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            // it's crucial to start the trace before the process starts and stop it after the benchmarked process stops to have all of the necessary events in the trace file!
            if (signal == HostSignal.BeforeProcessStart)
                Start(parameters);
            else if (signal == HostSignal.AfterProcessExit)
                Stop(parameters);
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!benchmarkToEtlFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                return Array.Empty<Metric>();

            // currently TraceLogParser parsers the counters metrics only. So if there are no counters configured, it makes no sense to parse the file
            if (!benchmarkToCounters.TryGetValue(results.BenchmarkCase, out var counters) || counters.IsEmpty())
                return Array.Empty<Metric>();

            return TraceLogParser.Parse(traceFilePath, counters);
        }

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToEtlFile.Any())
                return;

            logger.WriteLineInfo($"Exported {benchmarkToEtlFile.Count} trace file(s). Example:");
            logger.WriteLineInfo(benchmarkToEtlFile.Values.First());
        }

        private void Start(DiagnoserActionParameters parameters)
        {
            var counters = benchmarkToCounters[parameters.BenchmarkCase] = parameters.Config
                .GetHardwareCounters()
                .Select(counter => HardwareCounters.FromCounter(counter, config.IntervalSelectors.TryGetValue(counter, out var selector) ? selector : GetInterval))
                .ToArray();

            if (counters.Any()) // we need to enable the counters before starting the kernel session
                HardwareCounters.Enable(counters);

            try
            {
                kernelSession = new KernelSession(parameters, config, CreationTime).EnableProviders();
                if (config.CreateHeapSession)
                    heapSession = new HeapSession(parameters, config, CreationTime).EnableProviders();
                userSession = new UserSession(parameters, config, CreationTime).EnableProviders();
            }
            catch (Exception)
            {
                userSession?.Dispose();
                heapSession?.Dispose();
                kernelSession?.Dispose();

                throw;
            }
        }

        private void Stop(DiagnoserActionParameters parameters)
        {
            WaitForDelayedEvents();
            string userSessionFile;
            try
            {
                kernelSession.Stop();
                heapSession?.Stop();
                userSession.Stop();

                userSessionFile = userSession.FilePath;
            }
            finally
            {
                kernelSession.Dispose();
                heapSession?.Dispose();
                userSession.Dispose();
            }

            // Merge the 'primary' etl file X.etl (userSession) with any files that match .clr*.etl .user*.etl. and .kernel.etl.
            TraceEventSession.MergeInPlace(userSessionFile, TextWriter.Null);

            benchmarkToEtlFile[parameters.BenchmarkCase] = userSessionFile;
        }

        private static int GetInterval(ProfileSourceInfo info) => Math.Min(info.MaxInterval, Math.Max(info.MinInterval, info.Interval));

        /// <summary>
        /// ETW sessions receive events with a slight delay.
        /// This increases the likelihood that all relevant events are processed by the collection thread by the time we are done with the benchmark.
        /// </summary>
        private static void WaitForDelayedEvents() => Thread.Sleep(TimeSpan.FromMilliseconds(500));

        private IReadOnlyDictionary<BenchmarkCase, PmcStats> BuildPmcStats()
        {
            var builder = ImmutableDictionary.CreateBuilder<BenchmarkCase, PmcStats>();

            foreach (var benchmarkToCounter in benchmarkToCounters)
            {
                var uniqueCounters = benchmarkToCounter.Value.Select(x => x.Counter).Distinct().ToImmutableArray();

                var pmcStats = new PmcStats(
                    uniqueCounters,
                    counter => benchmarkToCounter.Value.Single(pmc => pmc.Counter == counter)
                );

                builder.Add(benchmarkToCounter.Key, pmcStats);
            }

            return builder.ToImmutable();
        }
    }
}