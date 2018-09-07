using System;
using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class JitDiagnoser : EtwDiagnoser<object>, IDiagnoser
    {
        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.JitTracing;

        protected override string SessionNamePrefix => "JitTracing";

        public abstract IEnumerable<string> Ids { get; }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeAnythingElse)
                Start(parameters);
            else if (signal == HostSignal.AfterAll)
                Stop();
        }

        public virtual IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();

        public void DisplayResults(ILogger outputLogger)
        {
            if (Logger.CapturedOutput.Count > 0)
                outputLogger.WriteLineHeader(new string('-', 20));
            foreach (var line in Logger.CapturedOutput)
                outputLogger.Write(line.Kind, line.Text);
        }
    }
}