using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    [AggressivelyOptimizeMethods]
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

        public async ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            foreach (var diagnoser in diagnosers)
            {
                // Do not use ConfigureAwait(false) here so that all diagnosers will be handled on the original context.
                await diagnoser.HandleAsync(signal, parameters, cancellationToken);
            }
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

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => diagnosers.ToAsyncEnumerable().SelectMany(diagnoser => diagnoser.ValidateAsync(validationParameters));
    }

    public sealed class CompositeInProcessDiagnoser(IReadOnlyList<IInProcessDiagnoser> inProcessDiagnosers)
    {
        public const string HeaderKey = "// InProcessDiagnoser";
        public const string ResultsKey = $"{HeaderKey}Results";

        public IReadOnlyList<IInProcessDiagnoser> InProcessDiagnosers { get; } = inProcessDiagnosers;

        public void DeserializeResults(int index, BenchmarkCase benchmarkCase, string results)
            => InProcessDiagnosers[index].DeserializeResults(benchmarkCase, results);
    }

    [AggressivelyOptimizeMethods]
    [UsedImplicitly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CompositeInProcessDiagnoserHandler(IReadOnlyList<InProcessDiagnoserRouter> routers, IHost host, RunMode runMode, InProcessDiagnoserActionArgs parameters)
    {
        public async ValueTask HandleAsync(BenchmarkSignal signal, CancellationToken cancellationToken)
        {
            if (runMode == RunMode.None)
            {
                return;
            }

            foreach (var router in routers)
            {
                if (router.ShouldHandle(runMode))
                {
                    // Do not use ConfigureAwait(false) here so that all handlers will be handled on the original context.
                    await router.handler.HandleAsync(signal, parameters, cancellationToken);
                }
            }

            if (signal is not (BenchmarkSignal.AfterEngine or BenchmarkSignal.SeparateLogic))
            {
                return;
            }

            foreach (var router in routers)
            {
                if (!router.ShouldHandle(runMode))
                {
                    continue;
                }

                var results = router.handler.SerializeResults();
                // Send header with the diagnoser index for routing, and line count of payload (user handler may include newlines in their serialized results).
                // Ideally we would simply use results.Length, write it directly to host, then the host reads the exact count of chars.
                // But WasmExecutor using StdOut does not support direct writes without newlines, so we need to instead use line count and prepend every line with CompositeInProcessDiagnoser.ResultsKey.
                var resultsLines = results.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                host.WriteLine($"{CompositeInProcessDiagnoser.HeaderKey} {router.index} {resultsLines.Length}");
                foreach (var line in resultsLines)
                {
                    host.WriteLine($"{CompositeInProcessDiagnoser.ResultsKey} {line}");
                }
            }
        }
    }
}