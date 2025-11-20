using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// Gets the type of the handler that will run in the separate benchmark process and its serialized config.
        /// </summary>
        /// <remarks>
        /// The handlerType must implement <see cref="IInProcessDiagnoserHandler"/> and have a publicly accessible default constructor.
        /// <para/>
        /// Return <see langword="default"/> to not run the diagnoser handler for the <paramref name="benchmarkCase"/>.
        /// </remarks>
        (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase);

        // GetSameProcessHandler is needed to prevent the handler type from being trimmed for InProcess toolchains.

        /// <summary>
        /// Gets the handler that will run in the same process.
        /// </summary>
        /// <remarks>
        /// Return <see langword="null"/> to not run the diagnoser handler for the <paramref name="benchmarkCase"/>.
        /// </remarks>
        IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase);

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