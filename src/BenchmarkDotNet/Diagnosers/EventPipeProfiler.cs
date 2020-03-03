using System;
using System.Collections.Generic;
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

        private readonly List<EventPipeProvider> eventPipeProviders = new List<EventPipeProvider>
        {
            new EventPipeProvider(EngineEventSource.SourceName, EventLevel.Informational, long.MaxValue) // mandatory provider to enable Engine events
        };

        private readonly LogCapture logger = new LogCapture();

        private Task collectingTask;

        // parameterless constructor required by DiagnosersLoader to support creating this profiler via console line args
        public EventPipeProfiler() { }

        /// <summary>
        /// Creates new instance of EventPipeProfiler
        /// </summary>
        /// <param name="profile">A named pre-defined set of provider configurations that allows common tracing scenarios to be specified succinctly.</param>
        /// <param name="providers">A list of EventPipe providers to be enabled.</param>
        public EventPipeProfiler(EventPipeProfile? profile = null, IReadOnlyCollection<EventPipeProvider> providers = null)
        {
            if (profile == null && (providers == null || !providers.Any()))
            {
                logger.WriteLine(LogKind.Info, "No profile or providers specified, defaulting to trace profile 'CpuSampling'");
                profile = EventPipeProfile.CpuSampling;
            }

            if (providers != null)
            {
                eventPipeProviders.AddRange(providers);
            }

            if (profile != null)
            {
                if (EventPipeProfileMapper.DotNetRuntimeProfiles.TryGetValue(profile.Value, out var selectedProfile))
                {
                    var newProvidersFromProfile = selectedProfile.Where(p => !eventPipeProviders.Any(r => r.Name.Equals(p.Name)));
                    eventPipeProviders.AddRange(newProvidersFromProfile);
                }
                else
                {
                    logger.WriteLine(LogKind.Error, $"Invalid profile name: {profile}");
                }
            }
        }

        public string ShortName => "EP";

        public IEnumerable<string> Ids => new[] { nameof(EventPipeProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraRun;

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

            EventPipeSession session;
            try
            {
                session = diagnosticsClient.StartEventPipeSession(eventPipeProviders, true);
            }
            catch (DiagnosticsClientException e)
            {
                logger.WriteLine(LogKind.Error, $"Unable to start a tracing session: {e}");
                return;
            }

            var fileName = ArtifactFileNameHelper.GetFilePath(parameters, DateTime.Now, "nettrace").EnsureFolderExists();
            benchmarkToTraceFile[parameters.BenchmarkCase] = fileName;

            collectingTask = Task.Run(() => CopyEventStreamToFile(session, fileName, logger));
        }

        private static void CopyEventStreamToFile(EventPipeSession session, string fileName, LogCapture logger)
        {
            try
            {
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
                benchmarkToTraceFile[results.BenchmarkCase] = SpeedScopeExporter.Convert(traceFilePath, logger);

            return Array.Empty<Metric>();
        }

        public void DisplayResults(ILogger resultLogger)
        {
            const string logSeparator = "--------------------";

            resultLogger.WriteLine();
            resultLogger.WriteLineHeader(logSeparator);

            if (logger.CapturedOutput.Any())
            {
                foreach (var line in logger.CapturedOutput)
                    resultLogger.Write(line.Kind, line.Text);

                resultLogger.WriteLine();
            }
            if (!benchmarkToTraceFile.Any())
                return;

            resultLogger.WriteLineInfo($"Exported {benchmarkToTraceFile.Count} trace file(s). Example:");
            resultLogger.WriteLineInfo(benchmarkToTraceFile.Values.First());

            resultLogger.WriteLineHeader(logSeparator);
        }
    }
}
