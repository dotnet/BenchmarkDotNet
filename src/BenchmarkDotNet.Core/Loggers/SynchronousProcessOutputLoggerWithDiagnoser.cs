using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class SynchronousProcessOutputLoggerWithDiagnoser
    {
        private readonly ILogger logger;
        private readonly Process process;
        private readonly Benchmark benchmark;
        private readonly IDiagnoser diagnoser;
        private readonly DiagnoserActionParameters diagnoserActionParameters;

        public SynchronousProcessOutputLoggerWithDiagnoser(ILogger logger, Process process, IDiagnoser diagnoser, Benchmark benchmark, IConfig config)
        {
            if (!process.StartInfo.RedirectStandardOutput)
            {
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            }

            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            this.benchmark = benchmark;
            diagnoserActionParameters = new DiagnoserActionParameters(
                process,
                benchmark,
                config);

            LinesWithResults = new List<string>();
            LinesWithExtraOutput = new List<string>();
        }

        internal List<string> LinesWithResults { get; }

        internal List<string> LinesWithExtraOutput { get; }

        internal void ProcessInput()
        {
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                logger.WriteLine(LogKind.Default, line);

                if (string.IsNullOrEmpty(line))
                    continue;

                if (!line.StartsWith("//"))
                {
                    LinesWithResults.Add(line);
                }
                else if (line == Engine.Signals.BeforeAnythingElse)
                {
                    diagnoser?.BeforeAnythingElse(diagnoserActionParameters);
                }
                else if (line == Engine.Signals.AfterGlobalSetup)
                {
                    diagnoser?.AfterGlobalSetup(diagnoserActionParameters);
                }
                else if (line == Engine.Signals.BeforeMainRun)
                {
                    diagnoser?.BeforeMainRun(diagnoserActionParameters);
                }
                else if (line == Engine.Signals.BeforeGlobalCleanup)
                {
                    diagnoser?.BeforeGlobalCleanup(diagnoserActionParameters);
                }
                else if (line == Engine.Signals.AfterAnythingElse)
                {
                    // TODO: notify AfterAnythingElse
                }
                else
                {
                    LinesWithExtraOutput.Add(line);
                }
            }
        }
    }
}