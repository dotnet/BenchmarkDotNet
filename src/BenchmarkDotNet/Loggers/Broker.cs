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
        private TcpListener? tcpListener;

        private enum Result
        {
            Success,
            EndOfStream,
            InvalidData,
            EarlyProcessExit,
        }

        public Broker(ILogger logger, Process process, IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, TcpListener tcpListener)
        {
            this.logger = logger;
            this.process = process;
            this.Diagnoser = diagnoser;
            this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
            this.tcpListener = tcpListener;
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

            Interlocked.Exchange(ref tcpListener, null)?.Stop();
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
            if (process.HasExited || this.tcpListener is not { } tcpListener)
                return Result.EarlyProcessExit;

            try
            {
#if NETSTANDARD2_0
                using var client = await tcpListener.AcceptTcpClientAsync();
#else
                TcpClient client;
                try
                {
                    using var cts = new CancellationTokenSource(TcpHost.ConnectionTimeout);
                    client = await tcpListener.AcceptTcpClientAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"The connection to the benchmark process timed out after {TcpHost.ConnectionTimeout}.");
                }
                using var _ = client;
#endif
                using var stream = client.GetStream();

                using StreamReader reader = new(stream, TcpHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
                // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
                using StreamWriter writer = new(stream, TcpHost.UTF8NoBOM, bufferSize: 1) { AutoFlush = true };

                while (true)
                {
                    var line = await reader.ReadLineAsync();
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
                            line = await reader.ReadLineAsync();
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

                        await writer.WriteLineAsync(Engine.Signals.Acknowledgment);

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
            catch (SocketException e) when (e.ErrorCode == 10004) // The socket was closed.
            {
                return Result.EarlyProcessExit;
            }
        }
    }
}
