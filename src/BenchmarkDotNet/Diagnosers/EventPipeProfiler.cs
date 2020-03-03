using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;

namespace BenchmarkDotNet.Diagnosers
{
    public class EventPipeProfiler : IProfiler
    {
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

        private readonly Dictionary<BenchmarkCase, string> benchmarkToTraceFile = new Dictionary<BenchmarkCase, string>();
        
        private readonly List<EventPipeProvider> eventPipeProviders = new List<EventPipeProvider>
        {
            new EventPipeProvider("BenchmarkDotNet.EngineEventSource", EventLevel.Informational, long.MaxValue) // mandatory provider to enable Engine events
        };

        private static readonly string LogSeparator = new string('-', 20);

        public static readonly EventPipeProfiler Default = new EventPipeProfiler();

        private readonly LogCapture logger = new LogCapture();

        private Task collectingTask;

        public IEnumerable<string> Ids => new[] { nameof(EventPipeProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraRun;

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
            var fileName = ArtifactFileNameHelper.GetFilePath(parameters, DateTime.Now, "nettrace");
            Path.GetDirectoryName(fileName).CreateIfNotExists();
            benchmarkToTraceFile[parameters.BenchmarkCase] = fileName;

            collectingTask = new Task(() =>
            {
                try
                {
                    using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[16 * 1024];

                        while (true)
                        {
                            int nBytesRead = session.EventStream.Read(buffer, 0, buffer.Length);
                            if (nBytesRead <= 0)
                                break;
                            fs.Write(buffer, 0, nBytesRead);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLine(LogKind.Error, $"An exception occurred during reading trace stream: {ex}");
                }
            });
            collectingTask.Start();
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            Task.WaitAll(collectingTask);

            if (benchmarkToTraceFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                ConvertToSpeedscope(results.BenchmarkCase, traceFilePath);

            return Array.Empty<Metric>();
        }
        
        public void DisplayResults(ILogger resultLogger)
        {
            resultLogger.WriteLine();
            resultLogger.WriteLineHeader(LogSeparator);

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

            resultLogger.WriteLineHeader(LogSeparator);
        }

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

        public string ShortName => "EventPipe";

        private void ConvertToSpeedscope(BenchmarkCase benchmarkCase, string traceFilePath)
        {
            var speedscopeFileName = Path.ChangeExtension(traceFilePath, "speedscope.json");

            try
            {
                ConvertToSpeedscope(traceFilePath, speedscopeFileName);
                benchmarkToTraceFile[benchmarkCase] = speedscopeFileName;
            }
            // Below comment come from https://github.com/dotnet/diagnostics/blob/2c23d3265dd8f642a8d6cf4bb8a135a5ff8b00c2/src/Tools/dotnet-trace/TraceFileFormatConverter.cs#L42
            // TODO: On a broken/truncated trace, the exception we get from TraceEvent is a plain System.Exception type because it gets caught and rethrown inside TraceEvent.
            // We should probably modify TraceEvent to throw a better exception.
            catch (Exception ex)
            {
                if (ex.ToString().Contains("Read past end of stream."))
                {
                    logger.WriteLine(LogKind.Info,
                        "Detected a potentially broken trace. Continuing with best-efforts to convert the trace, but resulting speedscope file may contain broken stacks as a result.");
                    ConvertToSpeedscope(traceFilePath, speedscopeFileName, true);
                    benchmarkToTraceFile[benchmarkCase] = speedscopeFileName;
                }
                else
                {
                    logger.WriteLine(LogKind.Error, $"An exception occurred during converting {traceFilePath} file to speedscope format: {ex}");
                }
            }
        }

        // Method copied from https://github.com/dotnet/diagnostics/blob/2c23d3265dd8f642a8d6cf4bb8a135a5ff8b00c2/src/Tools/dotnet-trace/TraceFileFormatConverter.cs#L64
        private static void ConvertToSpeedscope(string fileToConvert, string outputFilename, bool continueOnError = false)
        {
            var etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });
            using (var symbolReader = new SymbolReader(System.IO.TextWriter.Null) { SymbolPath = SymbolPath.MicrosoftSymbolServerPath })
            using (var eventLog = new TraceLog(etlxFilePath))
            {
                var stackSource = new MutableTraceEventStackSource(eventLog)
                {
                    OnlyManagedCodeStacks = true // EventPipe currently only has managed code stacks.
                };

                var computer = new SampleProfilerThreadTimeComputer(eventLog, symbolReader)
                {
                    IncludeEventSourceEvents = false // SpeedScope handles only CPU samples, events are not supported
                };
                computer.GenerateThreadTimeStacks(stackSource);

                SpeedScopeStackSourceWriter.WriteStackViewAsJson(stackSource, outputFilename);
            }

            if (File.Exists(etlxFilePath))
            {
                File.Delete(etlxFilePath);
            }
        }
    }
}
