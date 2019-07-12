using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Tracing;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class NativeMemoryDiagnoser : IDiagnoser
    {
        private static readonly string LogSeparator = new string('-', 20);
        internal readonly LogCapture Logger = new LogCapture();
        private readonly EtwProfiler etwProfiler;

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public NativeMemoryDiagnoser() => etwProfiler = new EtwProfiler(CreateDefaultConfig());

        public IEnumerable<string> Ids => new[] { nameof(NativeMemoryDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void DisplayResults(ILogger logger)
        {
            logger.WriteLineHeader(LogSeparator);
            foreach (var line in Logger.CapturedOutput)
                logger.Write(line.Kind, line.Text);
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) => etwProfiler.Handle(signal, parameters);

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!etwProfiler.BenchmarkToEtlFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                return Enumerable.Empty<Metric>();

            return new NativeMemoryLogParser(traceFilePath, results.BenchmarkCase, Logger).Parse();
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => etwProfiler.Validate(validationParameters);

        private static EtwProfilerConfig CreateDefaultConfig()
        {
            var kernelKeywords = KernelTraceEventParser.Keywords.Default | KernelTraceEventParser.Keywords.VirtualAlloc | KernelTraceEventParser.Keywords.VAMap;

            return new EtwProfilerConfig(
                performExtraBenchmarksRun: true,
                kernelKeywords: kernelKeywords,
                createHeapSession: true);
        }
    }
}