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
        /// Gets the data used to construct the <see cref="IInProcessDiagnoserHandler"/> that will run in the benchmark process.
        /// </summary>
        /// <remarks>
        /// The <see cref="InProcessDiagnoserHandlerData.HandlerType"/> must implement <see cref="IInProcessDiagnoserHandler"/> and have a publicly accessible default constructor.
        /// <para/>
        /// Return <see langword="default"/> to not run the diagnoser handler for the <paramref name="benchmarkCase"/>.
        /// </remarks>
        InProcessDiagnoserHandlerData GetHandlerData(BenchmarkCase benchmarkCase);

        /// <summary>
        /// Deserializes the results of the handler.
        /// </summary>
        void DeserializeResults(BenchmarkCase benchmarkCase, string serializedResults);
    }

    /// <summary>
    /// Represents a handler for an <see cref="IInProcessDiagnoser"/>.
    /// </summary>
    public interface IInProcessDiagnoserHandler
    {
        /// <summary>
        /// Initializes the handler with the serialized config from the host <see cref="IInProcessDiagnoser"/>.
        /// </summary>
        void Initialize(string? serializedConfig);

        /// <summary>
        /// Handles the signal from the benchmark.
        /// </summary>
        void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args);

        /// <summary>
        /// Serializes the results to be sent back to the host <see cref="IInProcessDiagnoser"/>.
        /// </summary>
        string SerializeResults();
    }
}