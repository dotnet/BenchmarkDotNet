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
using System.Xml.Linq;

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
        private readonly Dictionary<BenchmarkCase, string> benchmarkToCvTraceFile = [];
        private readonly Dictionary<BenchmarkCase, int> benchmarkToProcessId = [];

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public ConcurrencyVisualizerProfiler() => etwProfiler = new EtwProfiler(CreateDefaultConfig());

        [PublicAPI]
        public ConcurrencyVisualizerProfiler(EtwProfilerConfig config) => etwProfiler = new EtwProfiler(config);

        public string ShortName => "CV";

        public IEnumerable<string> Ids => [nameof(ConcurrencyVisualizerProfiler)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public void DisplayResults(ILogger logger)
        {
            if (!benchmarkToCvTraceFile.Any())
                return;

            logger.WriteLineInfo($"Exported {benchmarkToCvTraceFile.Count} CV trace file(s). Example:");
            logger.WriteLineInfo(benchmarkToCvTraceFile.Values.First());
            logger.WriteLineInfo("DO remember that this Diagnoser just tries to mimic the CVCollectionCmd.exe and you need to have Visual Studio with Concurrency Visualizer plugin installed to visualize the data.");
        }

        public async ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken)
        {
            await etwProfiler.HandleAsync(signal, parameters, cancellationToken).ConfigureAwait(false);

            // we need to remember process Id because we loose it when the process exits
            if (signal == HostSignal.AfterAll)
                benchmarkToProcessId[parameters.BenchmarkCase] = parameters.ProcessId;
            else if (signal == HostSignal.AfterProcessExit)
                benchmarkToCvTraceFile[parameters.BenchmarkCase] = CreateCvTraceFile(parameters);
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => etwProfiler.ProcessResults(results);

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters) => etwProfiler.ValidateAsync(validationParameters);

        private static EtwProfilerConfig CreateDefaultConfig()
        {
            var kernelKeywords = KernelTraceEventParser.Keywords.ImageLoad | KernelTraceEventParser.Keywords.Profile; // same as for EtwProfiler

            // following keywords come from decompiled "GetLocalTraceProviders" of CVCollectionService.exe
            // we don't use KernelTraceEventParser.Keywords.Dispatcher because it blows the CV Visualizer in VS, same goes for KernelTraceEventParser.Keywords.ThreadTime which I tried to experiment with
            kernelKeywords |= KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch;

            // following events were not enabled by default but I believe that they are important
            kernelKeywords |= KernelTraceEventParser.Keywords.DiskFileIO | KernelTraceEventParser.Keywords.DiskIO | KernelTraceEventParser.Keywords.DiskIOInit;
            kernelKeywords |= KernelTraceEventParser.Keywords.FileIO | KernelTraceEventParser.Keywords.FileIOInit;

            var providers = new (Guid providerGuid, TraceEventLevel providerLevel, ulong keywords, TraceEventProviderOptions? options)[]
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

            var trace = new XElement("ConcurrencyTrace",
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XAttribute("MajorVersion", "1"),
                new XAttribute("MinorVersion", "0"),
                new XElement("Config",
                    new XAttribute("MajorVersion", "1"),
                    new XAttribute("MinorVersion", "0"),
                    new XElement("DeleteEtlsAfterAnalysis", "false"),
                    new XElement("TraceLocation", directoryPath),
                    new XElement("Markers",
                        MarkerProvider("ConcurrencyVisualizer.Markers", ConcurrencyVisualizerMarkersId, "Low"),
                        MarkerProvider("System.Threading.Tasks", TplEtwProviderTraceEventParser.ProviderGuid, "Normal"),
                        MarkerProvider("System.Threading.Tasks.Dataflow", TplDataflowId, "Normal"),
                        MarkerProvider("System.Threading", TplSynchronizationId, "Normal"),
                        MarkerProvider("System.Collections.Concurrent", ManagedConcurrentCollectionsId, "Normal"),
                        MarkerProvider("System.Linq.Parallel", PlinqId, "Normal")),
                    new XElement("FilterConfig",
                        new XElement("CollectClrEvents", "true"),
                        new XElement("ClrCollectionOptions", "None"),
                        new XElement("CollectSampleEvents", "true"),
                        new XElement("CollectGpuEvents", "false"),
                        new XElement("CollectFileIO", "true")),
                    BufferSettings("UserBufferSettings"),
                    BufferSettings("KernelBufferSettings"),
                    GenerateCodeInfo(parameters)),
                new XElement("Pid", processId),
                new XElement("EtwSourceFileNames",
                    new XElement("EtwSourceFile", traceFileName)),
                new XElement("TraceProcesses"),
                new XElement("NtToDosMaps",
                    NtToDosNameMap(@"\??\", ""),
                    NtToDosNameMap(@"\SystemRoot\", @"C:\WINDOWS\"),
                    NtToDosNameMap(@"\Windows\", @"C:\WINDOWS\")));

            new XDocument(new XDeclaration("1.0", null, null), trace).Save(cvPathFile);

            return cvPathFile;
        }

        private static XElement MarkerProvider(string name, Guid id, string level)
            => new("MarkerProvider",
                new XAttribute("Name", name),
                new XAttribute("Guid", id),
                new XAttribute("Level", level));

        private static XElement BufferSettings(string name)
            => new(name,
                new XElement("BufferFlushTimer", "0"),
                new XElement("BufferSize", "256"),
                new XElement("MinimumBuffers", "512"),
                new XElement("MaximumBuffers", "1024"));

        private static XElement NtToDosNameMap(string ntName, string dosName)
            => new("NtToDosNameMap",
                new XAttribute("NtName", ntName),
                new XAttribute("DosName", dosName));

        private XElement GenerateCodeInfo(DiagnoserActionParameters parameters)
        {
            if (!parameters.Config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles))
                return new XElement("JustMyCode");

            var folderWithDlls = Path.GetDirectoryName(parameters.BenchmarkCase.Descriptor.Type.Assembly.Location);

            return new XElement("JustMyCode", new XElement("MyCodeDirectory", folderWithDlls));
        }
    }
}