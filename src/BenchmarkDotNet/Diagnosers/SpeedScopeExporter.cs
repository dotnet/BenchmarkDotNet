using BenchmarkDotNet.Loggers;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;
using System;
using System.IO;

namespace BenchmarkDotNet.Diagnosers
{
    internal static class SpeedScopeExporter
    {
        internal static string Convert(string traceFilePath, ILogger logger)
        {
            var speedscopeFileName = Path.ChangeExtension(traceFilePath, "speedscope.json");

            try
            {
                ConvertToSpeedscope(traceFilePath, speedscopeFileName);

                return speedscopeFileName;
            }
            // Below comment come from https://github.com/dotnet/diagnostics/blob/2c23d3265dd8f642a8d6cf4bb8a135a5ff8b00c2/src/Tools/dotnet-trace/TraceFileFormatConverter.cs#L42
            // TODO: On a broken/truncated trace, the exception we get from TraceEvent is a plain System.Exception type because it gets caught and rethrown inside TraceEvent.
            // We should probably modify TraceEvent to throw a better exception.
            catch (Exception ex) when (ex.ToString().Contains("Read past end of stream."))
            {
                logger.WriteLine(LogKind.Info,
                    "Detected a potentially broken trace. Continuing with best-efforts to convert the trace, but resulting speedscope file may contain broken stacks as a result.");
                ConvertToSpeedscope(traceFilePath, speedscopeFileName, true);

                return speedscopeFileName;
            }
            catch (Exception ex)
            {
                logger.WriteLine(LogKind.Error, $"An exception occurred during converting {traceFilePath} file to speedscope format: {ex}");

                return traceFilePath;
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
