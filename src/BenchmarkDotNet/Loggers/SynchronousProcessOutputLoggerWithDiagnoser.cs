using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class SynchronousProcessOutputLoggerWithDiagnoser
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly IDiagnoser diagnoser;
        private readonly NamedPipeServerStream namedPipeServer;
        private readonly DiagnoserActionParameters diagnoserActionParameters;

        public SynchronousProcessOutputLoggerWithDiagnoser(ILogger logger, Process process, IDiagnoser diagnoser,
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, NamedPipeServerStream namedPipeServer)
        {
            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            this.namedPipeServer = namedPipeServer;
            diagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

            LinesWithResults = new List<string>();
            LinesWithExtraOutput = new List<string>();
        }

        internal List<string> LinesWithResults { get; }

        internal List<string> LinesWithExtraOutput { get; }

        internal void ProcessInput()
        {
            namedPipeServer.WaitForConnection(); // wait for the benchmark app to connect first!

            using StreamReader reader = new (namedPipeServer, NamedPipeHost.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
            using StreamWriter writer = new (namedPipeServer, NamedPipeHost.UTF8NoBOM, bufferSize: 1);
            // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
            writer.AutoFlush = true;
            string line = null;

            while ((line = reader.ReadLine()) is not null)
            {
                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//"))
                {
                    LinesWithResults.Add(line);
                }
                else if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    diagnoser?.Handle(signal, diagnoserActionParameters);

                    writer.WriteLine(Engine.Signals.Acknowledgment);

                    if (signal == HostSignal.AfterAll)
                    {
                        // we have received the last signal so we can stop reading from the pipe
                        // if the process won't exit after this, its hung and needs to be killed
                        return;
                    }
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    LinesWithExtraOutput.Add(line);
                }
            }
        }
    }
}
