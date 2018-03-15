﻿using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnoser
    {
        IEnumerable<string> Ids { get; } 

        IEnumerable<IExporter> Exporters { get; }
            
        IColumnProvider GetColumnProvider();

        RunMode GetRunMode(Benchmark benchmark);

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
