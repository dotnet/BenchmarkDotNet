using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    /// <summary>
    /// a plugin which uses EtwProfiler to mimic the behavior of CVCollectionService.exe to produce not only an ETW trace file
    /// but also a CVTrace file which can be opened by Concurrency Visualizer plugin from Visual Studio
    /// </summary>
    public class ConcurrencyVisualizerProfiler : IProfiler
    {
        // following constants come from the decompiled "Microsoft.ConcurrencyVisualizer.Common.MarkerProviderConstants"
        private static readonly Guid PlinqId = new Guid("159eeeec-4a14-4418-a8fe-faabcd987887"); // "System.Linq.Parallel";
        private static readonly Guid TplDataflowId = new Guid("16f53577-e41d-43d4-b47e-c17025bf4025"); // "System.Threading.Tasks.Dataflow";
        private static readonly Guid TplSynchronizationId = new Guid("ec631d38-466b-4290-9306-834971ba0217"); // "System.Threading.Synchronization";
        private static readonly Guid ManagedConcurrentCollectionsId = new Guid("35167F8E-49B2-4B96-AB86-435B59336B5E"); // "System.Collections.Concurrent";
        private static readonly Guid ConcurrencyVisualizerMarkersId = new Guid("8D4925AB-505A-483b-A7E0-6F824A07A6F0"); // "ConcurrencyVisualizer.Markers";

        private readonly EtwProfiler etwProfiler;
        private readonly Dictionary<BenchmarkCase, string> benchmarkToCvTraceFile = new Dictionary<BenchmarkCase, string>();
        private readonly Dictionary<BenchmarkCase, int> benchmarkToProcessId = new Dictionary<BenchmarkCase, int>();

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public ConcurrencyVisualizerProfiler() => etwProfiler = new EtwProfiler(CreateDefaultConfig());

        [PublicAPI]
        public ConcurrencyVisualizerProfiler(EtwProfilerConfig config) => etwProfiler = new EtwProfiler(config);

        public string ShortName => "CV";

        public IEnumerable<string> Ids => new[] { nameof(ConcurrencyVisualizerProfiler) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToCvTraceFile.Any())
                return;

            logger.WriteLineInfo($"Exported {benchmarkToCvTraceFile.Count} CV trace file(s). Example:");
            logger.WriteLineInfo(benchmarkToCvTraceFile.Values.First());
            logger.WriteLineInfo("DO remember that this Diagnoser just tries to mimic the CVCollectionCmd.exe and you need to have Visual Studio with Concurrency Visualizer plugin installed to visualize the data.");
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            etwProfiler.Handle(signal, parameters);

            // we need to remember process Id because we loose it when the process exits
            if (signal == HostSignal.AfterAll)
                benchmarkToProcessId[parameters.BenchmarkCase] = parameters.Process.Id;
            else if (signal == HostSignal.AfterProcessExit)
                benchmarkToCvTraceFile[parameters.BenchmarkCase] = CreateCvTraceFile(parameters);
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => etwProfiler.ProcessResults(results);

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => etwProfiler.Validate(validationParameters);

        private static EtwProfilerConfig CreateDefaultConfig()
        {
            var kernelKeywords = KernelTraceEventParser.Keywords.ImageLoad | KernelTraceEventParser.Keywords.Profile; // same as for EtwProfiler

            // following keywords come from decompiled "GetLocalTraceProviders" of CVCollectionService.exe
            // we don't use KernelTraceEventParser.Keywords.Dispatcher because it blows the CV Visualizer in VS, same goes for KernelTraceEventParser.Keywords.ThreadTime which I tried to experiment with
            kernelKeywords |= KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch;

            // following events were not enabled by default but I believe that they are important
            kernelKeywords |= KernelTraceEventParser.Keywords.DiskFileIO | KernelTraceEventParser.Keywords.DiskIO | KernelTraceEventParser.Keywords.DiskIOInit;
            kernelKeywords |= KernelTraceEventParser.Keywords.FileIO | KernelTraceEventParser.Keywords.FileIOInit;

            var providers = new (Guid providerGuid, TraceEventLevel providerLevel, ulong keywords, TraceEventProviderOptions options)[]
            {
                // following keywords come from decompiled CVCollectionService.exe
                (ConcurrencyVisualizerMarkersId, TraceEventLevel.Verbose, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (TplDataflowId, TraceEventLevel.Informational, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (TplSynchronizationId, TraceEventLevel.Informational, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (ManagedConcurrentCollectionsId, TraceEventLevel.Informational, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (PlinqId, TraceEventLevel.Informational, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (ThreadPoolTraceEventParser.ProviderGuid, TraceEventLevel.Informational, EtwProfilerConfig.MatchAnyKeywords, new TraceEventProviderOptions { StacksEnabled = false }),
                (TplEtwProviderTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)TplEtwProviderTraceEventParser.Keywords.Default, new TraceEventProviderOptions { StacksEnabled = false }), // do NOT set it to verbose (VS crashes)
                // following values come from xunit-performance, were selected by the .NET Runtime Team
                (ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                    (ulong) (ClrTraceEventParser.Keywords.Exception
                             | ClrTraceEventParser.Keywords.GC
                             | ClrTraceEventParser.Keywords.Jit
                             | ClrTraceEventParser.Keywords.JitTracing // for the inlining events
                             | ClrTraceEventParser.Keywords.Loader
                             | ClrTraceEventParser.Keywords.NGen
                             | ClrTraceEventParser.Keywords.Threading // extra
                             | ClrTraceEventParser.Keywords.ThreadTransfer), // extra
                    new TraceEventProviderOptions { StacksEnabled = false }) // stacks are too expensive for our purposes
            };

            return new EtwProfilerConfig(
                performExtraBenchmarksRun: false,
                kernelKeywords: kernelKeywords,
                providers: providers);
        }

        private string CreateCvTraceFile(DiagnoserActionParameters parameters)
        {
            var traceFilePath = etwProfiler.BenchmarkToEtlFile[parameters.BenchmarkCase];
            var processId = benchmarkToProcessId[parameters.BenchmarkCase];

            var directoryPath = Path.GetDirectoryName(traceFilePath);
            var cvPathFile = Path.ChangeExtension(traceFilePath, ".CvTrace");
            var traceFileName = Path.GetFileName(traceFilePath);

            File.WriteAllText(cvPathFile,
$@"<?xml version=""1.0""?>
<ConcurrencyTrace xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" MajorVersion=""1"" MinorVersion=""0"">
  <Config MajorVersion=""1"" MinorVersion=""0"">
    <DeleteEtlsAfterAnalysis>false</DeleteEtlsAfterAnalysis>
    <TraceLocation>{directoryPath}</TraceLocation>
    <Markers>
      <MarkerProvider Name=""ConcurrencyVisualizer.Markers"" Guid=""{ConcurrencyVisualizerMarkersId}"" Level=""Low"" />
      <MarkerProvider Name=""System.Threading.Tasks"" Guid=""{TplEtwProviderTraceEventParser.ProviderGuid}"" Level=""Normal"" />
      <MarkerProvider Name=""System.Threading.Tasks.Dataflow"" Guid=""{TplDataflowId}"" Level=""Normal"" />
      <MarkerProvider Name=""System.Threading"" Guid=""{TplSynchronizationId}"" Level=""Normal"" />
      <MarkerProvider Name=""System.Collections.Concurrent"" Guid=""{ManagedConcurrentCollectionsId}"" Level=""Normal"" />
      <MarkerProvider Name=""System.Linq.Parallel"" Guid=""{PlinqId}"" Level=""Normal"" />
    </Markers>
    <FilterConfig>
      <CollectClrEvents>true</CollectClrEvents>
      <ClrCollectionOptions>None</ClrCollectionOptions>
      <CollectSampleEvents>true</CollectSampleEvents>
      <CollectGpuEvents>false</CollectGpuEvents>
      <CollectFileIO>true</CollectFileIO>
    </FilterConfig>
    <UserBufferSettings>
      <BufferFlushTimer>0</BufferFlushTimer>
      <BufferSize>256</BufferSize>
      <MinimumBuffers>512</MinimumBuffers>
      <MaximumBuffers>1024</MaximumBuffers>
    </UserBufferSettings>
    <KernelBufferSettings>
      <BufferFlushTimer>0</BufferFlushTimer>
      <BufferSize>256</BufferSize>
      <MinimumBuffers>512</MinimumBuffers>
      <MaximumBuffers>1024</MaximumBuffers>
    </KernelBufferSettings>
    {GenerateCodeInfo(parameters)}
  </Config>
  <Pid>{processId}</Pid>
  <EtwSourceFileNames>
    <EtwSourceFile>{traceFileName}</EtwSourceFile>
  </EtwSourceFileNames>
  <TraceProcesses />
  <NtToDosMaps>
    <NtToDosNameMap NtName=""\??\"" DosName="""" />
    <NtToDosNameMap NtName=""\SystemRoot\"" DosName=""C:\WINDOWS\"" />
    <NtToDosNameMap NtName=""\Windows\"" DosName=""C:\WINDOWS\"" />
  </NtToDosMaps>
</ConcurrencyTrace>
");

            return cvPathFile;
        }

        private string GenerateCodeInfo(DiagnoserActionParameters parameters)
        {
            if (!parameters.Config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles))
                return "<JustMyCode />";

            var folderWithDlls = Path.GetDirectoryName(parameters.BenchmarkCase.Descriptor.Type.Assembly.Location);

            return $"<JustMyCode><MyCodeDirectory>{folderWithDlls}</MyCodeDirectory></JustMyCode>";
        }
    }
}