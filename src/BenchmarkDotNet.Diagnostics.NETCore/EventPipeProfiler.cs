using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;

namespace BenchmarkDotNet.Diagnostics.NETCore
{


    public class EventPipeProfiler : IProfiler
    {
        private readonly Dictionary<BenchmarkCase, string> benchmarkToTraceFile = new Dictionary<BenchmarkCase, string>();

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
            if (signal == HostSignal.BeforeAnythingElse)
            {
                var defaultProvider = new[]
                {
                    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
                    new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long) ClrTraceEventParser.Keywords.Default)
                };

                var diagnosticsClient = new DiagnosticsClient(parameters.Process.Id);

                EventPipeSession session;
                try
                {
                    session = diagnosticsClient.StartEventPipeSession(defaultProvider, true);
                }
                catch (DiagnosticsClientException e)
                {
                    logger.WriteLine(LogKind.Error, $"Unable to start a tracing session: {e}");
                    return;
                }

                var fileName = Path.Combine(parameters.Config.ArtifactsPath, GetFilePath(parameters, DateTime.Now));
                benchmarkToTraceFile[parameters.BenchmarkCase] = fileName;

               collectingTask = new Task(() =>
                {
                    try
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

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
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            Task.WaitAll(collectingTask);
          
            if (!benchmarkToTraceFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                return Array.Empty<Metric>();
            
            var speedscopeFileName = Path.ChangeExtension(traceFilePath, "speedscope.json");

            try
            {
                ConvertToSpeedscope(traceFilePath, speedscopeFileName);
                benchmarkToTraceFile[results.BenchmarkCase] = speedscopeFileName;
            }
            // Below comment come from https://github.com/dotnet/diagnostics/blob/2c23d3265dd8f642a8d6cf4bb8a135a5ff8b00c2/src/Tools/dotnet-trace/TraceFileFormatConverter.cs#L42
            // TODO: On a broken/truncated trace, the exception we get from TraceEvent is a plain System.Exception type because it gets caught and rethrown inside TraceEvent.
            // We should probably modify TraceEvent to throw a better exception.
            catch (Exception ex)
            {
                if (ex.ToString().Contains("Read past end of stream."))
                {
                    logger.WriteLine(LogKind.Info, "Detected a potentially broken trace. Continuing with best-efforts to convert the trace, but resulting speedscope file may contain broken stacks as a result.");
                    ConvertToSpeedscope(traceFilePath, speedscopeFileName, true);
                    benchmarkToTraceFile[results.BenchmarkCase] = speedscopeFileName;
                }
                else
                {
                    logger.WriteLine(LogKind.Error, $"An exception occurred during converting {traceFilePath} file to speedscope format: {ex}");
                }
            }
            return Array.Empty<Metric>();
        }

        public void DisplayResults(ILogger resultLogger)
        {
            resultLogger.WriteLine();
            resultLogger.WriteLineHeader(LogSeparator);

            foreach (var line in logger.CapturedOutput)
                resultLogger.Write(line.Kind, line.Text);

            resultLogger.WriteLine();

            if (!benchmarkToTraceFile.Any())
                return;

            resultLogger.WriteLineInfo($"Exported {benchmarkToTraceFile.Count} trace file(s). Example:");
            resultLogger.WriteLineInfo(benchmarkToTraceFile.Values.First());

            resultLogger.WriteLineHeader(LogSeparator);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            // TODO Validation if test is in .net core 3.0+

            yield break;
        }

        public string ShortName => "EventPipe";

        //TODO DRY issue. this method is similar to GetFilePath method from Session.cs.
        private string GetFilePath(DiagnoserActionParameters details, DateTime creationTime)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(details.BenchmarkCase)}";

            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => job.GetToolchain()).Distinct().Count() > 1)
                fileName += $"-{details.BenchmarkCase.Job.Environment.Runtime?.Name ?? details.BenchmarkCase.GetToolchain()?.Name ?? details.BenchmarkCase.Job.Id}";

            fileName += $"-{creationTime:yyyyMMdd-HHmmss}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            return $"{fileName}.nettrace";
        }

        //TODO DRY issue. this method come from Session.cs.
        private string EnsureFolderExists(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return filePath;
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

                var computer = new SampleProfilerThreadTimeComputer(eventLog, symbolReader);
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
