using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class SynchronousProcessOutputLoggerWithDiagnoser
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly IDiagnoser diagnoser;
        private readonly DiagnoserActionParameters diagnoserActionParameters;

        public SynchronousProcessOutputLoggerWithDiagnoser(ILogger logger, Process process, IDiagnoser diagnoser, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId)
        {
            if (!process.StartInfo.RedirectStandardOutput)
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            if (!(process.StartInfo.RedirectStandardInput || benchmarkCase.GetRuntime() is WasmRuntime))
                throw new NotSupportedException("set RedirectStandardInput to true first");

            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            diagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

            LinesWithResults = new List<string>();
            LinesWithExtraOutput = new List<string>();
        }

        internal List<string> LinesWithResults { get; }

        internal List<string> LinesWithExtraOutput { get; }

        internal void ProcessInput()
        {
            // Peek -1 or 0 can indicate internal error.
            while (!process.StandardOutput.EndOfStream && process.StandardOutput.Peek() > 0)
            {
                // ReadLine() can actually return string.Empty and null as valid values.
                string line = process.StandardOutput.ReadLine();

                if (line == null)
                    continue;

                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//"))
                {
                    LinesWithResults.Add(line);
                }
                else if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    diagnoser?.Handle(signal, diagnoserActionParameters);

                    if (process.StartInfo.RedirectStandardInput)
                    {
                        process.StandardInput.WriteLine(Engine.Signals.Acknowledgment);
                    }

                    if (signal == HostSignal.AfterAll)
                    {
                        // we have received the last signal so we can stop reading the output
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
