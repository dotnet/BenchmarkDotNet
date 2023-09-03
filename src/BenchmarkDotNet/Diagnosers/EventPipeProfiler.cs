using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.NETCore.Client;

namespace BenchmarkDotNet.Diagnosers
{
    public class EventPipeProfiler : IProfiler
    {
        public static readonly EventPipeProfiler Default = new EventPipeProfiler();

        private readonly Dictionary<BenchmarkCase, string> benchmarkToTraceFile = new Dictionary<BenchmarkCase, string>();
        private readonly ImmutableHashSet<EventPipeProvider> eventPipeProviders;
        private readonly bool performExtraBenchmarksRun;

        private Task collectingTask;

        // parameterless constructor required by DiagnosersLoader to support creating this profiler via console line args
        // we use performExtraBenchmarksRun = false for better first user experience
        public EventPipeProfiler() :this(profile: EventPipeProfile.CpuSampling, performExtraBenchmarksRun: false) { }

        /// <summary>
        /// Creates a new instance of EventPipeProfiler
        /// </summary>
        /// <param name="profile">A named pre-defined set of provider configurations that allows common tracing scenarios to be specified succinctly.</param>
        /// <param name="providers">A list of EventPipe providers to be enabled.</param>
        /// <param name="performExtraBenchmarksRun">if set to true, benchmarks will be executed one more time with the profiler attached. If set to false, there will be no extra run but the results will contain overhead. True by default.</param>
        public EventPipeProfiler(EventPipeProfile profile = EventPipeProfile.CpuSampling, IReadOnlyCollection<EventPipeProvider>? providers = null, bool performExtraBenchmarksRun = true)
        {
            this.performExtraBenchmarksRun = performExtraBenchmarksRun;
            eventPipeProviders = MapToProviders(profile, providers);
        }

        public string ShortName => "EP";

        public IEnumerable<string> Ids => new[] { nameof(EventPipeProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => performExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var runtime = benchmark.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance);

                if (runtime.RuntimeMoniker < RuntimeMoniker.NetCoreApp30)
                {
                    yield return new ValidationError(true, $"{nameof(EventPipeProfiler)} supports only .NET Core 3.0+", benchmark);
                }
            }
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal != HostSignal.BeforeAnythingElse)
                return;

            var diagnosticsClient = new DiagnosticsClient(parameters.Process.Id);

            EventPipeSession session = diagnosticsClient.StartEventPipeSession(eventPipeProviders, true);

            var fileName = ArtifactFileNameHelper.GetTraceFilePath(parameters, DateTime.Now, "nettrace").EnsureFolderExists();
            benchmarkToTraceFile[parameters.BenchmarkCase] = fileName;

            collectingTask = Task.Run(() => CopyEventStreamToFile(session, fileName, parameters.Config.GetCompositeLogger()));
        }

        private static void CopyEventStreamToFile(EventPipeSession session, string fileName, ILogger logger)
        {
            try
            {
                using (session)
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[16 * 1024];
                    int bytesRead = 0;

                    while ((bytesRead = session.EventStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }

                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                logger.WriteLine(LogKind.Error, $"An exception occurred during reading trace stream: {ex}");
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            Task.WaitAll(collectingTask);

            if (benchmarkToTraceFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                benchmarkToTraceFile[results.BenchmarkCase] = SpeedScopeExporter.Convert(traceFilePath, results.BenchmarkCase.Config.GetCompositeLogger());

            return Array.Empty<Metric>();
        }

        public void DisplayResults(ILogger resultLogger)
        {
            if (!benchmarkToTraceFile.Any())
                return;

            resultLogger.WriteLineInfo($"Exported {benchmarkToTraceFile.Count} trace file(s). Example:");
            resultLogger.WriteLineInfo(benchmarkToTraceFile.Values.First());
        }

        internal static ImmutableHashSet<EventPipeProvider> MapToProviders(EventPipeProfile profile, IReadOnlyCollection<EventPipeProvider> providers)
        {
            var uniqueProviders = ImmutableHashSet.CreateBuilder<EventPipeProvider>(EventPipeProviderEqualityComparer.Instance);

            if (providers != null)
            {
                foreach (var userProvidedProfile in providers)
                {
                    uniqueProviders.Add(userProvidedProfile);
                }
            }

            var selectedProfile = EventPipeProfileMapper.DotNetRuntimeProfiles[profile];
            foreach (var provider in selectedProfile)
            {
                uniqueProviders.Add(provider);
            }

            // mandatory provider to enable Engine events
            uniqueProviders.Add(new EventPipeProvider(EngineEventSource.SourceName, EventLevel.Informational, long.MaxValue));
            return uniqueProviders.ToImmutable();
        }

        private sealed class EventPipeProviderEqualityComparer : IEqualityComparer<EventPipeProvider>
        {
            internal static readonly IEqualityComparer<EventPipeProvider> Instance = new EventPipeProviderEqualityComparer();

            public bool Equals(EventPipeProvider x, EventPipeProvider y) => x.Name.Equals(y.Name);

            public int GetHashCode(EventPipeProvider obj) => obj.Name.GetHashCode();
        }
    }
}
