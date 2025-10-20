using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public sealed class CompositeDiagnoser : IDiagnoser
    {
        internal readonly ImmutableHashSet<IDiagnoser> diagnosers;

        public CompositeDiagnoser(ImmutableHashSet<IDiagnoser> diagnosers)
            => this.diagnosers = diagnosers;

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
            => throw new InvalidOperationException("Should never be called for Composite Diagnoser");

        public IEnumerable<string> Ids
            => diagnosers.SelectMany(d => d.Ids);

        public IEnumerable<IExporter> Exporters
            => diagnosers.SelectMany(diagnoser => diagnoser.Exporters);

        public IEnumerable<IAnalyser> Analysers
            => diagnosers.SelectMany(diagnoser => diagnoser.Analysers);

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.Handle(signal, parameters);
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
            => diagnosers.SelectMany(diagnoser => diagnoser.ProcessResults(results));

        public void DisplayResults(ILogger logger)
        {
            foreach (var diagnoser in diagnosers)
            {
                logger.WriteLineHeader($"// * Diagnostic Output - {diagnoser.Ids.Single()} *");
                diagnoser.DisplayResults(logger);
                logger.WriteLine();
            }
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => diagnosers.SelectMany(diagnoser => diagnoser.Validate(validationParameters));
    }

    public sealed class CompositeInProcessDiagnoser(IReadOnlyList<IInProcessDiagnoser> inProcessDiagnosers)
    {
        public const string HeaderKey = "// InProcessDiagnoser";
        public const string ResultsKey = $"{HeaderKey}Results";

        public IEnumerable<string> GetHandlersSourceCode(BenchmarkCase benchmarkCase)
            => inProcessDiagnosers
                .Select((d, i) => d.GetHandlerSourceCode(benchmarkCase, i))
                .Where(s => !string.IsNullOrEmpty(s));

        public IReadOnlyList<IInProcessDiagnoserHandler> GetInProcessHandlers(BenchmarkCase benchmarkCase)
            => [.. inProcessDiagnosers
                .Select((d, i) => d.GetHandler(benchmarkCase, i))
                .WhereNotNull()];

        public void DeserializeResults(int index, BenchmarkCase benchmarkCase, string results)
            => inProcessDiagnosers[index].DeserializeResults(benchmarkCase, results);
    }

    [UsedImplicitly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CompositeInProcessDiagnoserHandler(IReadOnlyList<IInProcessDiagnoserHandler> handlers, IHost host, RunMode runMode, InProcessDiagnoserActionArgs parameters)
    {
        public void Handle(BenchmarkSignal signal)
        {
            if (runMode == RunMode.None)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                if (handler.RunMode == runMode)
                {
                    handler.Handle(signal, parameters);
                }
            }

            if (signal != BenchmarkSignal.AfterEngine)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                if (handler.RunMode != runMode)
                {
                    continue;
                }

                var results = handler.SerializeResults();
                // Send header with the diagnoser index for routing, and line count of payload (user handler may include newlines in their serialized results).
                // Ideally we would simply use results.Length, write it directly to host, then the host reads the exact count of chars.
                // But WasmExecutor does not use Broker, and reads all output, so we need to instead use line count and prepend every line with CompositeInProcessDiagnoser.ResultsKey.
                var resultsLines = results.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                host.WriteLine($"{CompositeInProcessDiagnoser.HeaderKey} {handler.Index} {resultsLines.Length}");
                foreach (var line in resultsLines)
                {
                    host.WriteLine($"{CompositeInProcessDiagnoser.ResultsKey} {line}");
                }
            }
        }
    }
}