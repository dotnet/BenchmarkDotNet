using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnoser
    {
        IEnumerable<string> Ids { get; }

        IEnumerable<IExporter> Exporters { get; }

        IEnumerable<IAnalyser> Analysers { get; }

        IColumnProvider GetColumnProvider();

        RunMode GetRunMode(BenchmarkCase benchmarkCase);

        void Handle(HostSignal signal, DiagnoserActionParameters parameters);

        void ProcessResults(DiagnoserResults results);

        void DisplayResults(ILogger logger);

        IEnumerable<ValidationError> Validate(ValidationParameters validationParameters);
    }

    public interface IConfigurableDiagnoser<TConfig> : IDiagnoser
    {
        IConfigurableDiagnoser<TConfig> Configure(TConfig config);
    }
}