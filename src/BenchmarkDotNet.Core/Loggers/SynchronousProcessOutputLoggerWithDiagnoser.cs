using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private bool diagnosticsAlreadyRun = false;

        public SynchronousProcessOutputLoggerWithDiagnoser(ILogger logger, Process process, IDiagnoser diagnoser, Benchmark benchmark)
        {
            if (!process.StartInfo.RedirectStandardOutput)
            {
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            }

            this.logger = logger;
            this.process = process;
            this.diagnoser = diagnoser;
            this.benchmark = benchmark;

            Lines = new List<string>();
        }

        internal List<string> Lines { get; }

        internal void ProcessInput()
        {
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                {
                    Lines.Add(line);
                }

                // This is important so the Diagnoser can know the [Benchmark] methods will have run and (e.g.) it can do a Memory Dump
                if (diagnosticsAlreadyRun || !line.StartsWith(IterationMode.MainWarmup.ToString()))
                {
                    continue;
                }

                try
                {
                    diagnoser?.AfterBenchmarkHasRun(benchmark, process);
                }
                finally
                {
                    // Always set this, even if something went wrong, otherwise we will try on every run of a benchmark batch
                    diagnosticsAlreadyRun = true;
                }
            }
        }
    }
}