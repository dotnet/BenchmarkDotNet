using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class JitDiagnoser : EtwDiagnoser<object>, IDiagnoser
    {
        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.JitTracing;

        protected override string SessionNamePrefix => "JitTracing";

        public abstract IEnumerable<string> Ids { get; }
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;

        public void BeforeAnythingElse(DiagnoserActionParameters parameters) => Start(parameters);

        public void AfterGlobalSetup(DiagnoserActionParameters _) { }

        public void BeforeMainRun(DiagnoserActionParameters _) { }

        public void BeforeGlobalCleanup(DiagnoserActionParameters parameters) => Stop();

        public virtual void ProcessResults(Benchmark benchmark, BenchmarkReport report) { }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Enumerable.Empty<ValidationError>();

        public void DisplayResults(ILogger outputLogger)
        {
            if (Logger.CapturedOutput.Count > 0)
                outputLogger.WriteLineHeader(new string('-', 20));
            foreach (var line in Logger.CapturedOutput)
                outputLogger.Write(line.Kind, line.Text);
        }
    }
}