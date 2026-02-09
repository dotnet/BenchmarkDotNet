using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

#nullable enable

namespace BenchmarkDotNet.Loggers
{
    internal class Broker
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly CompositeInProcessDiagnoser compositeInProcessDiagnoser;
        private readonly NamedPipeServerStream pipe;

        public Broker(ILogger logger, Process process, IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, NamedPipeServerStream pipe)
        {
            this.logger = logger;
            this.process = process;
            this.Diagnoser = diagnoser;
            this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
            this.pipe = pipe;
            DiagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
        }

        internal IDiagnoser? Diagnoser { get; }

        internal DiagnoserActionParameters DiagnoserActionParameters { get; }

        internal List<string> Results { get; } = [];

        internal List<string> PrefixedOutput { get; } = [];

        internal async ValueTask ProcessData()
        {
            // When the process fails to start, there is no pipe to read from.
            // If we try to read from such pipe, the read blocks and BDN hangs.
            // We can't use async methods with cancellation tokens because Anonymous Pipes don't support async IO.

            // Usually, this property is not set yet.
            if (process.HasExited)
            {
                return;
            }

            await ProcessDataCore();
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            process.Exited -= OnProcessExited;

            // Dispose the pipe to let reading from pipe finish with EOF and avoid a resource leak.
            pipe.Dispose();
        }

        private async ValueTask ProcessDataCore()
        {
            await pipe.WaitForConnectionAsync();

            using StreamReader reader = new(pipe, NamedPipeHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            using StreamWriter writer = new(pipe, NamedPipeHost.UTF8NoBOM, bufferSize: 1) { AutoFlush = true };

            while (await reader.ReadLineAsync() is { } line)
            {
                // TODO: implement Silent mode here
                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//"))
                {
                    Results.Add(line);
                }
                // Keep in sync with WasmExecutor and InProcessHost.
                else if (line.StartsWith(CompositeInProcessDiagnoser.HeaderKey))
                {
                    // Something like "// InProcessDiagnoser 0 1"
                    string[] lineItems = line.Split(' ');
                    int diagnoserIndex = int.Parse(lineItems[2]);
                    int resultsLinesCount = int.Parse(lineItems[3]);
                    var resultsStringBuilder = new StringBuilder();
                    for (int i = 0; i < resultsLinesCount;)
                    {
                        // Strip the prepended "// InProcessDiagnoserResults ".
                        line = reader.ReadLine()!.Substring(CompositeInProcessDiagnoser.ResultsKey.Length + 1);
                        resultsStringBuilder.Append(line);
                        if (++i < resultsLinesCount)
                        {
                            resultsStringBuilder.AppendLine();
                        }
                    }
                    compositeInProcessDiagnoser.DeserializeResults(diagnoserIndex, DiagnoserActionParameters.BenchmarkCase, resultsStringBuilder.ToString());
                }
                else if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    Diagnoser?.Handle(signal, DiagnoserActionParameters);

                    await writer.WriteLineAsync(Engine.Signals.Acknowledgment);

                    if (signal == HostSignal.AfterAll)
                    {
                        // we have received the last signal so we can stop reading from the pipe
                        // if the process won't exit after this, its hung and needs to be killed
                        process.Exited -= OnProcessExited;
                        return;
                    }
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    PrefixedOutput.Add(line);
                }
            }
        }
    }
}
