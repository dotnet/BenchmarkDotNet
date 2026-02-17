using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class Broker : IDisposable
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly CompositeInProcessDiagnoser compositeInProcessDiagnoser;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly IpcListener ipcListener;

        private enum Result
        {
            Success,
            EndOfStream,
            InvalidData,
            EarlyProcessExit,
        }

        public Broker(ILogger logger, Process process, IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IpcListener ipcListener)
        {
            this.logger = logger;
            this.process = process;
            this.Diagnoser = diagnoser;
            this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
            this.ipcListener = ipcListener;
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

            cancellationTokenSource.Cancel(throwOnFirstException: false);
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
            if (process.HasExited || cancellationTokenSource.IsCancellationRequested)
                return Result.EarlyProcessExit;

            try
            {
                using var ipcConnection = await ipcListener.AcceptConnection(cancellationTokenSource.Token);

                while (true)
                {
                    var line = await ipcConnection.ReadLineAsync(cancellationTokenSource.Token);

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

                    // Keep in sync with InProcessHost.

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
                            line = await ipcConnection.ReadLineAsync(cancellationTokenSource.Token);

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

                        await ipcConnection.WriteLineAsync(Engine.Signals.Acknowledgment);

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
            catch (IOException e) when (e.InnerException is SocketException se && IsEarlyExitCode(se.SocketErrorCode))
            {
                return Result.EarlyProcessExit;
            }
            catch (SocketException e) when (IsEarlyExitCode(e.SocketErrorCode))
            {
                return Result.EarlyProcessExit;
            }
            catch (OperationCanceledException)
            {
                return Result.EarlyProcessExit;
            }

            static bool IsEarlyExitCode(SocketError error)
                => error is SocketError.ConnectionReset
                or SocketError.Shutdown
                or SocketError.OperationAborted;
        }
    }
}
