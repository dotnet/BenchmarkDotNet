using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

#nullable enable

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>Host API for in-process benchmarks.</summary>
    /// <seealso cref="IHost"/>
    [AggressivelyOptimizeMethods]
    internal sealed class InProcessHost : IHost
    {
        private readonly ILogger logger;
        private readonly IDiagnoser? diagnoser;
        private readonly DiagnoserActionParameters? diagnoserActionParameters;
        private readonly List<string> inProcessDiagnoserLines = [];

        /// <summary>Creates a new instance of <see cref="InProcessHost"/>.</summary>
        /// <param name="benchmarkCase">Current benchmark.</param>
        /// <param name="logger">Logger for informational output.</param>
        /// <param name="diagnoser">Diagnosers, if attached.</param>
        public InProcessHost(BenchmarkCase benchmarkCase, ILogger logger, IDiagnoser? diagnoser)
        {
            if (benchmarkCase == null)
                throw new ArgumentNullException(nameof(benchmarkCase));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.diagnoser = diagnoser;
            Config = benchmarkCase.Config;

            if (diagnoser != null)
                diagnoserActionParameters = new DiagnoserActionParameters(
                    Process.GetCurrentProcess(),
                    benchmarkCase,
                    default);
        }

        /// <summary>Results of the run.</summary>
        /// <value>Results of the run.</value>
        public RunResults RunResults { get; private set; }

        /// <summary>Current config</summary>
        public IConfig Config { get; set; }

        /// <summary>Passes text to the host.</summary>
        /// <param name="message">Text to write.</param>
        public void WriteAsync(string message) => logger.Write(message);

        /// <summary>Passes new line to the host.</summary>
        public void WriteLine() => logger.WriteLine();

        /// <summary>Passes text (new line appended) to the host.</summary>
        /// <param name="message">Text to write.</param>
        public void WriteLine(string message)
        {
            logger.WriteLine(message);
            if (message.StartsWith(CompositeInProcessDiagnoser.HeaderKey)) // Captures both header and results
            {
                inProcessDiagnoserLines.Add(message);
            }
        }

        /// <summary>Sends notification signal to the host.</summary>
        /// <param name="hostSignal">The signal to send.</param>
        public void SendSignal(HostSignal hostSignal) => diagnoser?.Handle(hostSignal, diagnoserActionParameters!);

        public void SendError(string message) => logger.WriteLine(LogKind.Error, $"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        /// <summary>Submits run results to the host.</summary>
        /// <param name="runResults">The run results.</param>
        public void ReportResults(RunResults runResults)
        {
            RunResults = runResults;

            using (var w = new StringWriter())
            {
                runResults.Print(w);
                logger.Write(w.GetStringBuilder().ToString());
            }
        }

        // Keep in sync with Broker and WasmExecutor.
        internal void HandleInProcessDiagnoserResults(BenchmarkCase benchmarkCase, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
        {
            var linesEnumerator = inProcessDiagnoserLines.GetEnumerator();
            while (linesEnumerator.MoveNext())
            {
                // Something like "// InProcessDiagnoser 0 1"
                var line = linesEnumerator.Current;
                string[] lineItems = line.Split(' ');
                int diagnoserIndex = int.Parse(lineItems[2]);
                int resultsLinesCount = int.Parse(lineItems[3]);
                var resultsStringBuilder = new StringBuilder();
                for (int i = 0; i < resultsLinesCount;)
                {
                    // Strip the prepended "// InProcessDiagnoserResults ".
                    bool movedNext = linesEnumerator.MoveNext();
                    Debug.Assert(movedNext);
                    line = linesEnumerator.Current.Substring(CompositeInProcessDiagnoser.ResultsKey.Length + 1);
                    resultsStringBuilder.Append(line);
                    if (++i < resultsLinesCount)
                    {
                        resultsStringBuilder.AppendLine();
                    }
                }
                compositeInProcessDiagnoser.DeserializeResults(diagnoserIndex, benchmarkCase, resultsStringBuilder.ToString());
            }
        }

        public void Dispose()
        {
            // do nothing on purpose
        }
    }
}