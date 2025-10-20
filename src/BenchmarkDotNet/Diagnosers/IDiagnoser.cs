using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnoser
    {
        IEnumerable<string> Ids { get; }

        IEnumerable<IExporter> Exporters { get; }

        IEnumerable<IAnalyser> Analysers { get; }

        RunMode GetRunMode(BenchmarkCase benchmarkCase);

        void Handle(HostSignal signal, DiagnoserActionParameters parameters);

        IEnumerable<Metric> ProcessResults(DiagnoserResults results);

        void DisplayResults(ILogger logger);

        IEnumerable<ValidationError> Validate(ValidationParameters validationParameters);
    }

    public interface IConfigurableDiagnoser<in TConfig> : IDiagnoser
    {
        [PublicAPI] IConfigurableDiagnoser<TConfig> Configure(TConfig config);
    }

    /// <summary>
    /// Represents a diagnoser that will be handled in the same process as the benchmarks.
    /// </summary>
    public interface IInProcessDiagnoser : IDiagnoser
    {
        /// <summary>
        /// Gets the C# source code used to instantiate the handler in the benchmark process.
        /// </summary>
        /// <remarks>
        /// The source code must be a single expression.
        /// </remarks>
        string GetHandlerSourceCode(BenchmarkCase benchmarkCase, int index);

        /// <summary>
        /// Gets the handler for the same process.
        /// </summary>
        IInProcessDiagnoserHandler GetHandler(BenchmarkCase benchmarkCase, int index);

        /// <summary>
        /// Deserializes the results of the handler.
        /// </summary>
        void DeserializeResults(BenchmarkCase benchmarkCase, string results);
    }

    /// <summary>
    /// Represents a handler for an <see cref="IInProcessDiagnoser"/>.
    /// </summary>
    public interface IInProcessDiagnoserHandler
    {
        /// <summary>
        /// The index of the diagnoser.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// The <see cref="Diagnosers.RunMode"/> of the diagnoser for the benchmark.
        /// </summary>
        RunMode RunMode { get; }

        /// <summary>
        /// Handles the signal from the benchmark.
        /// </summary>
        void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs parameters);

        /// <summary>
        /// Serializes the results to be sent back to the host <see cref="IInProcessDiagnoser"/>.
        /// </summary>
        string SerializeResults();
    }
}