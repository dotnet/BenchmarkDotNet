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
}