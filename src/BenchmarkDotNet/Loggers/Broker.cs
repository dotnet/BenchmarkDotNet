using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

#nullable enable

namespace BenchmarkDotNet.Loggers
{
    internal class Broker : IDisposable
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly CompositeInProcessDiagnoser compositeInProcessDiagnoser;
        private NamedPipeServerStream? inPipe;
        private NamedPipeServerStream? outPipe;

        private enum Result
        {
            Success,
            EndOfStream,
            InvalidData,
            EarlyProcessExit,
        }

        public Broker(ILogger logger, Process process, IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, NamedPipeServerStream inPipe, NamedPipeServerStream outPipe)
        {
            this.logger = logger;
            this.process = process;
            this.Diagnoser = diagnoser;
            this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
            this.inPipe = inPipe;
            this.outPipe = outPipe;
            DiagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
        }

        internal IDiagnoser? Diagnoser { get; }

        internal DiagnoserActionParameters DiagnoserActionParameters { get; }

        internal List<string> Results { get; } = [];

        internal List<string> PrefixedOutput { get; } = [];

        public void Dispose()
        {
            process.Exited -= OnProcessExited;

            // Dispose the pipes to let reading from pipe finish with EOF and avoid a resource leak.
            Interlocked.Exchange(ref inPipe, null)?.Dispose();
            Interlocked.Exchange(ref outPipe, null)?.Dispose();
        }

        private void OnProcessExited(object? sender, EventArgs e)
            => Dispose();

        internal async ValueTask ProcessData()
        {
            var result = await ProcessDataCore();
            if (result != Result.Success)
            {
                logger.WriteLineError($"ProcessData operation is interrupted by {result}.");
            }
        }

        private async ValueTask<Result> ProcessDataCore()
        {
            if (process.HasExited || this.inPipe is not { } inPipe || this.outPipe is not { } outPipe)
                return Result.EarlyProcessExit;

            try
            {
                using var cts = new CancellationTokenSource(NamedPipesHost.PipeConnectionTimeout);
                await Task.WhenAll([
                    inPipe.WaitForConnectionAsync(cts.Token),
                    outPipe.WaitForConnectionAsync(cts.Token)]
                );
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"The connection to the benchmark process timed out after {NamedPipesHost.PipeConnectionTimeout}.");
            }
            // If the process exited before the connection was established such that the pipe is disposed from the exited handler,
            // it throws IOException on Windows or SocketException on Unix.
            catch (Exception e) when (e is IOException or SocketException)
            {
                return Result.EarlyProcessExit;
            }

            using StreamReader reader = new(inPipe, NamedPipesHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            using StreamWriter writer = new(outPipe, NamedPipesHost.UTF8NoBOM, bufferSize: 1) { AutoFlush = true };

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    return Result.EndOfStream;

                // TODO: implement Silent mode here
                logger.WriteLine(LogKind.Default, line);

                // Handle normal log.
                if (!line.StartsWith("//"))
                {
                    Results.Add(line);
                    continue;
                }

                // Keep in sync with WasmExecutor and InProcessHost.

                // Handle line prefixed with "// InProcessDiagnoser "
                if (line.StartsWith(CompositeInProcessDiagnoser.HeaderKey))
                {
                    // Something like "// InProcessDiagnoser 0 1"
                    string[] lineItems = line.Split(' ');
                    int diagnoserIndex = int.Parse(lineItems[2]);
                    int resultsLinesCount = int.Parse(lineItems[3]);
                    var resultsStringBuilder = new StringBuilder();
                    for (int i = 0; i < resultsLinesCount;)
                    {
                        line = reader.ReadLine();
                        if (line == null)
                            return Result.EndOfStream;

                        if (!line.StartsWith($"{CompositeInProcessDiagnoser.ResultsKey} "))
                            return Result.InvalidData;

                        // Strip the prepended "// InProcessDiagnoserResults ".
                        line = line.Substring(CompositeInProcessDiagnoser.ResultsKey.Length + 1);
                        resultsStringBuilder.Append(line);
                        if (++i < resultsLinesCount)
                        {
                            resultsStringBuilder.AppendLine();
                        }
                    }
                    compositeInProcessDiagnoser.DeserializeResults(diagnoserIndex, DiagnoserActionParameters.BenchmarkCase, resultsStringBuilder.ToString());
                    continue;
                }

                // Handle HostSignal data
                if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    Diagnoser?.Handle(signal, DiagnoserActionParameters);

                    writer.WriteLine(Engine.Signals.Acknowledgment);

                    if (signal == HostSignal.AfterAll)
                    {
                        // we have received the last signal so we can stop reading from the pipe
                        // if the process won't exit after this, its hung and needs to be killed
                        return Result.Success;
                    }

                    continue;
                }

                // Other line that have "//" prefix.
                PrefixedOutput.Add(line);
            }
        }
    }
}
