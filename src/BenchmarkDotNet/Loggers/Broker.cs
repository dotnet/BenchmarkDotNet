using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
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
        private readonly AnonymousPipeServerStream inputFromBenchmark, acknowledgments;

        private enum Result
        {
            Success,
            EndOfStream,
            InvalidData,
        }

        public Broker(ILogger logger, Process process, IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, AnonymousPipeServerStream inputFromBenchmark, AnonymousPipeServerStream acknowledgments)
        {
            this.logger = logger;
            this.process = process;
            this.Diagnoser = diagnoser;
            this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
            this.inputFromBenchmark = inputFromBenchmark;
            this.acknowledgments = acknowledgments;
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

            // Dispose all the pipes to let reading from pipe finish with EOF and avoid a resource leak.
            DisposeLocalCopyOfClientHandles();
            inputFromBenchmark.Dispose();
            acknowledgments.Dispose();
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            DisposeLocalCopyOfClientHandles();
        }

        private void DisposeLocalCopyOfClientHandles()
        {
            inputFromBenchmark.DisposeLocalCopyOfClientHandle();
            acknowledgments.DisposeLocalCopyOfClientHandle();
        }

        internal void ProcessData()
        {
            // When the process fails to start, there is no pipe to read from.
            // If we try to read from such pipe, the read blocks and BDN hangs.
            // We can't use async methods with cancellation tokens because Anonymous Pipes don't support async IO.

            // Usually, this property is not set yet.
            if (process.HasExited)
                return;

            var result = ProcessDataBlocking();
            if (result != Result.Success)
                logger.WriteLineError($"ProcessData operation is interrupted by {result}.");
        }

        private Result ProcessDataBlocking()
        {
            using StreamReader reader = new(inputFromBenchmark, AnonymousPipesHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            using StreamWriter writer = new(acknowledgments, AnonymousPipesHost.UTF8NoBOM, bufferSize: 1);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            writer.AutoFlush = true;

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

                    if (signal == HostSignal.BeforeAnythingElse)
                    {
                        // The client has connected, we no longer need to keep the local copy of client handle alive.
                        // This allows server to detect that child process is done and hence avoid resource leak.
                        // Full explanation: https://stackoverflow.com/a/39700027
                        DisposeLocalCopyOfClientHandles();
                    }
                    else if (signal == HostSignal.AfterAll)
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
