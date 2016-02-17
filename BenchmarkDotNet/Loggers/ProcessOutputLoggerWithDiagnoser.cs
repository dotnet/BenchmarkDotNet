using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Loggers
{
    internal class ProcessOutputLoggerWithDiagnoser : ProcessOutputLogger
    {
        private readonly Benchmark benchmark;
        private readonly IDiagnoser diagnoser;

        private bool diagnosticsAlreadyRun = false;

        public ProcessOutputLoggerWithDiagnoser(ILogger logger, Process process, IDiagnoser diagnoser, Benchmark benchmark) : base(logger, process)
        {
            if (!process.StartInfo.RedirectStandardOutput)
            {
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            }

            this.diagnoser = diagnoser;
            this.benchmark = benchmark;

            Lines = new List<string>();
        }

        internal List<string> Lines { get; }

        protected override void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            var line = dataReceivedEventArgs.Data;
            if (line == null)
            {
                return;
            }

            base.ProcessOnOutputDataReceived(sender, dataReceivedEventArgs); // let's put it in log first

            if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                Lines.Add(line);

            // This is important so the Diagnoser can know the [Benchmark] methods will have run and (e.g.) it can do a Memory Dump
            if (diagnosticsAlreadyRun == false && line.StartsWith(IterationMode.MainWarmup.ToString()))
            {
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