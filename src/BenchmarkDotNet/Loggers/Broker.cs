using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Portability;
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

        public Broker(ILogger logger, Process process, IDiagnoser diagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, AnonymousPipeServerStream inputFromBenchmark, AnonymousPipeServerStream acknowledgments)
        {
            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            this.inputFromBenchmark = inputFromBenchmark;
            this.acknowledgments = acknowledgments;
            diagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

            Results = new List<string>();
            PrefixedOutput = new List<string>();
        }

        internal List<string> Results { get; }

        internal List<string> PrefixedOutput { get; }

        internal void ProcessData()
        {
            // Starting new processes on Windows is way more expensive when compared to Unix.
            int milliseconds = RuntimeInformation.IsWindows() ? 500 : 250;
            if (process.WaitForExit(milliseconds))
            {
                // When the process fails to start, there is no pipe to read from.
                // If we try to read from such pipe, the read blocks and BDN hangs.
                // We can't use async methods with cancellation tokens because Anonymous Pipes don't support async IO.
                // That is why we just wait a little for the process to fail.
                return;
            }

            using StreamReader reader = new (inputFromBenchmark, AnonymousPipesHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            using StreamWriter writer = new (acknowledgments, AnonymousPipesHost.UTF8NoBOM, bufferSize: 1);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            writer.AutoFlush = true;
            string line = null;

            while ((line = reader.ReadLine()) is not null)
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
