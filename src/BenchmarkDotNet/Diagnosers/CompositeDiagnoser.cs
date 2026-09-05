using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
                await diagnoser.HandleAsync(signal, parameters, cancellationToken).ConfigureAwait();
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

        // Written out rather than composed with async LINQ - see CompositeValidator.ValidateAsync for why.
        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => ValidateAsyncCore(validationParameters);

        private async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var diagnoser in diagnosers)
            {
#pragma warning disable CA2007
                await foreach (var error in diagnoser.ValidateAsync(validationParameters).ConfigureAwait(cancellationToken))
#pragma warning restore CA2007
                {
                    yield return error;
                }
            }
        }
    }

    public sealed class CompositeInProcessDiagnoser(IReadOnlyList<IInProcessDiagnoser> inProcessDiagnosers)
    {
        public const string HeaderKey = "// InProcessDiagnoser";
        public const string ResultsKey = $"{HeaderKey}Results";

        public IReadOnlyList<IInProcessDiagnoser> InProcessDiagnosers { get; } = inProcessDiagnosers;

        private readonly ConcurrentDictionary<BenchmarkCase, IReadOnlyList<InProcessDiagnoserHandlerData>> handlerDataByBenchmark = new();

        // The handler data is queried while building a benchmark (code generation, the Roslyn reference set, build-error
        // explanation). This composite is shared across the benchmark cases of a run info, so memoize per benchmark case -
        // each diagnoser's GetHandlerData (which may do real work) then runs at most once per benchmark.
        public IReadOnlyList<InProcessDiagnoserHandlerData> GetHandlerData(BenchmarkCase benchmarkCase)
            => handlerDataByBenchmark.GetOrAdd(benchmarkCase, bc => [.. InProcessDiagnosers.Select(diagnoser => diagnoser.GetHandlerData(bc))]);

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
                    await router.handler.HandleAsync(signal, parameters, cancellationToken).ConfigureAwait();
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
