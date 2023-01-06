using System;
using System.Collections.Generic;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class JitDiagnoser<TStats> : EtwDiagnoser<TStats>, IDiagnoser where TStats : new()
    {
        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.JitTracing;

        protected override string SessionNamePrefix => "JitTracing";

        public override RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public abstract IEnumerable<string> Ids { get; }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeAnythingElse)
                Start(parameters);
            else if (signal == HostSignal.AfterAll)
                Stop();
        }

        public virtual IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (!RuntimeInformation.IsWindows())
            {
                yield return new ValidationError(true, $"{GetType().Name} is supported only on Windows");
            }
        }

        public void DisplayResults(ILogger outputLogger)
        {
            if (Logger.CapturedOutput.Count > 0)
                outputLogger.WriteLineHeader(new string('-', 20));
            foreach (var line in Logger.CapturedOutput)
                outputLogger.Write(line.Kind, line.Text);
        }
    }
}