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
    public class NativeMemoryProfiler : IProfiler
    {
        private readonly LogCapture logger = new LogCapture();

        private readonly EtwProfiler etwProfiler;

        public string ShortName => "NativeMemory";

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public NativeMemoryProfiler() => etwProfiler = new EtwProfiler(CreateDefaultConfig());

        public IEnumerable<string> Ids => new[] { nameof(NativeMemoryProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger resultLogger)
        {
            if (etwProfiler.BenchmarkToEtlFile.Any())
            {
                resultLogger.WriteLineInfo($"Exported {etwProfiler.BenchmarkToEtlFile.Count} trace file(s). Example:");
                resultLogger.WriteLineInfo(etwProfiler.BenchmarkToEtlFile.Values.First());
            }

            foreach (var line in logger.CapturedOutput)
                resultLogger.Write(line.Kind, line.Text);
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) => etwProfiler.Handle(signal, parameters);

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!etwProfiler.BenchmarkToEtlFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                return Enumerable.Empty<Metric>();

            return new NativeMemoryLogParser(traceFilePath, results.BenchmarkCase, logger, results.BuildResult.ArtifactsPaths.ProgramName).Parse();
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => etwProfiler.Validate(validationParameters);

        private static EtwProfilerConfig CreateDefaultConfig()
        {
            // We add VirtualAlloc because we want to catch low level VirtualAlloc and VirtualFree calls.
            // We should add also VAMap which means that we want to log mapping of files into memory.
            var kernelKeywords = KernelTraceEventParser.Keywords.VirtualAlloc | KernelTraceEventParser.Keywords.VAMap;

            return new EtwProfilerConfig(
                performExtraBenchmarksRun: true,
                kernelKeywords: kernelKeywords,
                createHeapSession: true);
        }
    }
}