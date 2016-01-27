using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Plugins.Diagnosers;

namespace BenchmarkDotNet.Plugins.Loggers
{
    internal class ProcessOutputLoggerWithDiagnoser : ProcessOutputLogger
    {
        private readonly Benchmark benchmark;
        private readonly IBenchmarkDiagnoser diagnoser;

        private bool codeAlreadyExtracted = false;

        public ProcessOutputLoggerWithDiagnoser(IBenchmarkLogger logger, Process process, IBenchmarkDiagnoser diagnoser, Benchmark benchmark) : base(logger, process)
        {
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

            // Wait until we know "Warmup" is happening, and then dissassemble the process
            if (codeAlreadyExtracted == false && line.StartsWith("Warmup ") && !line.StartsWith("WarmupIdle "))
            {
                try
                {
                    diagnoser.Print(benchmark, process, logger);
                }
                finally
                {
                    // Always set this, even if something went wrong, otherwise we will try on every run of a benchmark batch
                    codeAlreadyExtracted = true;
                }
            }
        }
    }
}