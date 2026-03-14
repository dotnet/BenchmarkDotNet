using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    [AggressivelyOptimizeMethods]
    internal sealed class InProcessHost : IHost
    {
        private readonly ILogger logger;
        private readonly IDiagnoser diagnoser;
        private readonly DiagnoserActionParameters? diagnoserActionParameters;
        private readonly List<string> inProcessDiagnoserLines = [];

        public InProcessHost(BenchmarkCase benchmarkCase, ILogger logger, IDiagnoser diagnoser, CancellationToken cancellationToken)
        {
            this.logger = logger;
            this.diagnoser = diagnoser;
            Config = benchmarkCase.Config;
            CancellationToken = cancellationToken;

            if (diagnoser != null)
                diagnoserActionParameters = new DiagnoserActionParameters(
                    Process.GetCurrentProcess(),
                    benchmarkCase,
                    default);
        }

        public RunResults RunResults { get; private set; }

        public IConfig Config { get; set; }

        public CancellationToken CancellationToken { get; private set; }

        public void Dispose()
        {
            // do nothing on purpose
        }

        public void WriteLine() => logger.WriteLine();

        public void WriteLine(string message)
        {
            logger.WriteLine(message);
            if (message.StartsWith(CompositeInProcessDiagnoser.HeaderKey)) // Captures both header and results
            {
                inProcessDiagnoserLines.Add(message);
            }
        }

        public void SendError(string message) => logger.WriteLine(LogKind.Error, $"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

        public void ReportResults(RunResults runResults)
        {
            RunResults = runResults;

            runResults.Print(this);
        }

        public ValueTask SendSignalAsync(HostSignal hostSignal)
            => diagnoser.HandleAsync(hostSignal, diagnoserActionParameters!, CancellationToken);

        public ValueTask Yield() => new();

        // Keep in sync with Broker.
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
    }
}