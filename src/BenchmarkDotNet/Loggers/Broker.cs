using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class Broker
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly IDiagnoser diagnoser;
        private readonly AnonymousPipeServerStream inputFromBenchmark, acknowledgments;
        private readonly DiagnoserActionParameters diagnoserActionParameters;
        private readonly ManualResetEvent finished;

        public Broker(ILogger logger, Process process, IDiagnoser diagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, AnonymousPipeServerStream inputFromBenchmark, AnonymousPipeServerStream acknowledgments)
        {
            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            this.inputFromBenchmark = inputFromBenchmark;
            this.acknowledgments = acknowledgments;
            diagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);
            finished = new ManualResetEvent(false);

            Results = new List<string>();
            PrefixedOutput = new List<string>();

            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
        }

        internal List<string> Results { get; }

        internal List<string> PrefixedOutput { get; }

        internal void ProcessData()
        {
            // When the process fails to start, there is no pipe to read from.
            // If we try to read from such pipe, the read blocks and BDN hangs.
            // We can't use async methods with cancellation tokens because Anonymous Pipes don't support async IO.

            // Usually, this property is not set yet.
            if (process.HasExited)
            {
                return;
            }

            Task.Run(ProcessDataBlocking);

            finished.WaitOne();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            process.Exited -= OnProcessExited;

            // Dispose all the pipes to let reading from pipe finish with EOF and avoid a reasource leak.
            inputFromBenchmark.DisposeLocalCopyOfClientHandle();
            inputFromBenchmark.Dispose();
            acknowledgments.DisposeLocalCopyOfClientHandle();
            acknowledgments.Dispose();

            finished.Set();
        }

        private void ProcessDataBlocking()
        {
            using StreamReader reader = new (inputFromBenchmark, AnonymousPipesHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            using StreamWriter writer = new (acknowledgments, AnonymousPipesHost.UTF8NoBOM, bufferSize: 1);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            writer.AutoFlush = true;

            while (reader.ReadLine() is { } line)
            {
                // TODO: implement Silent mode here
                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//"))
                {
                    Results.Add(line);
                }
                else if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    diagnoser?.Handle(signal, diagnoserActionParameters);

                    writer.WriteLine(Engine.Signals.Acknowledgment);

                    if (signal == HostSignal.BeforeAnythingElse)
                    {
                        // The client has connected, we no longer need to keep the local copy of client handle alive.
                        // This allows server to detect that child process is done and hence avoid resource leak.
                        // Full explanation: https://stackoverflow.com/a/39700027
                        inputFromBenchmark.DisposeLocalCopyOfClientHandle();
                        acknowledgments.DisposeLocalCopyOfClientHandle();
                    }
                    else if (signal == HostSignal.AfterAll)
                    {
                        // we have received the last signal so we can stop reading from the pipe
                        // if the process won't exit after this, its hung and needs to be killed
                        process.Exited -= OnProcessExited;
                        finished.Set();
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
