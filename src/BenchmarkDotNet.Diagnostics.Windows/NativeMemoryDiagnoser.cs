using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.PerfView;
using BenchmarkDotNet.Diagnostics.Windows.Tracing;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Address = System.UInt64;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class NativeMemoryDiagnoser : IDiagnoser
    {
        private static readonly string LogSeparator = new string('-', 20);
        internal readonly LogCapture Logger = new LogCapture();
        private readonly EtwProfiler etwProfiler;

        [PublicAPI] // parameterless ctor required by DiagnosersLoader to support creating this profiler via console line args
        public NativeMemoryDiagnoser()
        {
            etwProfiler = new EtwProfiler(CreateDefaultConfig());
        }

        public IEnumerable<string> Ids => new[] { nameof(NativeMemoryDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void DisplayResults(ILogger logger)
        {
            logger.WriteLineHeader(new string('-', 20));
            foreach (var line in Logger.CapturedOutput)
                logger.Write(line.Kind, line.Text);
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            etwProfiler.Handle(signal, parameters);
        }

        private class NativeAllocation
        {
            public string Type { get; set; }
            public long Size { get; set; }
        }
        private (long totalMemory, long memoryLeak) CreateCvTraceFile(BenchmarkCase parameters)
        {
            (long totalMemory, long memoryLeak) result = (0, 0);

            var traceFilePath = etwProfiler.BenchmarkToEtlFile[parameters];
            var etlxFile = Path.ChangeExtension(traceFilePath, ".etlx");
            var options = new TraceLogOptions();
            TraceLog.CreateFromEventTraceLogFile(traceFilePath, etlxFile, options);

            using (var eventLog = new TraceLog(etlxFile))
            {
                TraceEvents events = eventLog.Events;

                var eventSource = events.GetSource();

                var bdnEventsParser = new EngineEventLogParser(eventSource);

                var start = false;
                var isFirstActualStartEnd = false;
                StringBuilder bud = new StringBuilder();

                long totalOperation = 0;

                bdnEventsParser.WorkloadActualStart += data =>
                {
                    if (!isFirstActualStartEnd)
                    {
                        start = true;
                    }

                    bud.AppendLine($"WorkloadActualStart {data.ProcessID}, {data.TimeStampRelativeMSec}, {IterationMode.Workload}, {data.TotalOperations}");
                    totalOperation = data.TotalOperations;
                };
                bdnEventsParser.WorkloadActualStop += data =>
                {
                    bud.AppendLine($"WorkloadActualStop {data.ProcessID}, {data.TimeStampRelativeMSec}, {IterationMode.Workload}, {data.TotalOperations}");
                    start = false;
                    isFirstActualStartEnd = true;
                };

                var heapParser = new HeapTraceProviderTraceEventParser(eventSource);
                // We index by heap address and then within the heap we remember the allocation stack
                var heaps = new Dictionary<Address, Dictionary<Address, NativeAllocation>>();
                Dictionary<Address, NativeAllocation> lastHeapAllocs = null;

                // These three variables are used in the local function GetAllocationType defined below.
                // and are used to look up type names associated with the native allocations.   
                var loadedModules = new Dictionary<TraceModuleFile, NativeSymbolModule>();
                var allocationTypeNames = new Dictionary<CallStackIndex, string>();
                var symReader = GetSymbolReader(traceFilePath, SymbolReaderOptions.CacheOnly);

                Address lastHeapHandle = 0;

                long cumMetric = 0;
                long totalAllocation = 0;

                heapParser.HeapTraceAlloc += delegate (HeapAllocTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }
                    bud.AppendLine($"WorkloadActualStop {data.ProcessID}, {data.TimeStampRelativeMSec}, {IterationMode.Workload}, {data.AllocSize}");

                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                        allocs = GetHeap(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);

                    var callStackIndex = data.CallStackIndex();

                    // Add the 'Type ALLOCATION_TYPE' if available.  
                    string allocationType = GetAllocationType(callStackIndex);
                    if (allocationType != null)
                    {
                        bud.AppendLine($"allocationType={allocationType}");
                    }

                    allocs[data.AllocAddress] = new NativeAllocation() { Size = data.AllocSize, Type = allocationType };

                    cumMetric += data.AllocSize;
                    totalAllocation += data.AllocSize;


                    /*****************************************************************************/
                    // Performs a stack crawl to match the best typename to this allocation. 
                    // Returns null if no typename was found.
                    // This updates loadedModules and allocationTypeNames. It reads symReader/eventLog.
                    string GetAllocationType(CallStackIndex csi)
                    {
                        if (!allocationTypeNames.TryGetValue(csi, out var typeName))
                        {
                            const int frameLimit = 25; // typically you need about 10 frames to get out of the OS functions 
                                                       // to get to a frame that has type information.   We'll search up this many frames
                                                       // before giving up on getting type information for the allocation.  

                            int frameCount = 0;
                            for (var current = csi; current != CallStackIndex.Invalid && frameCount < frameLimit; current = eventLog.CallStacks.Caller(current), frameCount++)
                            {
                                var module = eventLog.CodeAddresses.ModuleFile(eventLog.CallStacks.CodeAddressIndex(current));
                                if (module == null)
                                    continue;

                                if (!loadedModules.TryGetValue(module, out var symbolModule))
                                {
                                    loadedModules[module] = symbolModule =
                                        (module.PdbSignature != Guid.Empty
                                            ? symReader.FindSymbolFilePath(module.PdbName, module.PdbSignature, module.PdbAge, module.FilePath)
                                            : symReader.FindSymbolFilePathForModule(module.FilePath)) is string pdb
                                        ? symReader.OpenNativeSymbolFile(pdb)
                                        : null;
                                }

                                typeName = symbolModule?.GetTypeForHeapAllocationSite(
                                        (uint)(eventLog.CodeAddresses.Address(eventLog.CallStacks.CodeAddressIndex(current)) - module.ImageBase)
                                    ) ?? typeName;

                            }
                            allocationTypeNames[csi] = typeName;
                        }
                        return typeName;
                    }
                };
                heapParser.HeapTraceFree += delegate (HeapFreeTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }
                    bud.AppendLine($"HeapTraceFree {data.ProcessID}, {data.TimeStampRelativeMSec}, {data.FreeAddress}");
                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = GetHeap(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    NativeAllocation alloc;
                    if (allocs.TryGetValue(data.FreeAddress, out alloc))
                    {
                        cumMetric -= alloc.Size;

                        allocs.Remove(data.FreeAddress);
                    }
                };
                heapParser.HeapTraceReAlloc += delegate (HeapReallocTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }
                    // Reallocs that actually move stuff will cause a Alloc and delete event
                    // so there is nothing to do for those events.  But when the address is
                    // the same we need to resize 
                    if (data.OldAllocAddress != data.NewAllocAddress)
                    {
                        return;
                    }
                    bud.AppendLine($"HeapTraceReAlloc {data.ProcessID}, {data.TimeStampRelativeMSec}, {data.OldAllocAddress}");

                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = GetHeap(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    // This is a clone of the Free code 
                    NativeAllocation alloc;
                    if (allocs.TryGetValue(data.OldAllocAddress, out alloc))
                    {
                        cumMetric -= alloc.Size;

                        allocs.Remove(data.OldAllocAddress);
                    }

                    // This is a clone of the Alloc code (sigh don't clone code)
                    allocs[data.NewAllocAddress] = new NativeAllocation() { Size = data.NewAllocSize, Type = alloc?.Type };

                    cumMetric += data.NewAllocSize;
                };
                heapParser.HeapTraceDestroy += delegate (HeapTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }
                    bud.AppendLine($"HeapTraceDestroy {data.ProcessID}, {data.TimeStampRelativeMSec}");

                    // Heap is dieing, kill all objects in it.   
                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = GetHeap(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    foreach (var alloc in allocs.Values)
                    {
                        // TODO this is a clone of the free code.  
                        cumMetric -= alloc.Size;
                    }
                };

                eventSource.Process();

                Logger.WriteLine();
                Logger.WriteLineHeader(LogSeparator);
                Logger.WriteLineInfo($"{parameters.DisplayInfo}");
                Logger.WriteLineHeader(LogSeparator);

                result.totalMemory = totalAllocation / totalOperation;
                result.memoryLeak = cumMetric / totalOperation;

                Logger.WriteLine($"Total native memory allocated: {result.totalMemory:n3}");
                if (cumMetric != 0)
                {
                    Logger.WriteLine($"Total memory leak: {result.memoryLeak:n3}");
                }
                var heapInfoList = heaps.Select(h => new { Address = h.Key, h.Value.Count, types = h.Value.Values });
                foreach (var item in heapInfoList.Where(p => p.Count > 0))
                {
                    Logger.WriteLine("heap address: " + item.Address);
                    Logger.WriteLine($"Count of not deallocated object: {item.Count / totalOperation}");

                    //This should be the same as {cumMetric / totalOperation:n3}
                    //Logger.WriteLine($"Not deallocated objects size: {item.types.Sum(p => p.Size) / totalOperation:n3}");
                    var groups = item.types.GroupBy(p => p.Type).Select(p => new { Type = p.Key, Size = p.Sum(s => s.Size) });
                    Logger.WriteLine($"Not deallocated objects types:");
                    foreach (var type in groups)
                    {
                        Logger.WriteLine($"{type.Type} = {type.Size / totalOperation:n3}");
                    }

                }
            }

            return result;
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!etwProfiler.BenchmarkToEtlFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                yield break;

            var result = CreateCvTraceFile(results.BenchmarkCase);

            yield return new Metric(new NativeMemoryDescriptor(), result.totalMemory);
            yield return new Metric(new NativeMemoryLeakDescriptor(), result.memoryLeak);
        } 

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => etwProfiler.Validate(validationParameters);

        /// <summary>
        /// Implements a simple one-element cache for find the heap to look in.  
        /// </summary>
        private static Dictionary<Address, NativeAllocation> GetHeap(Address heapHandle, Dictionary<Address, Dictionary<Address, NativeAllocation>> heaps, ref Dictionary<Address, NativeAllocation> lastHeapAllocs, ref Address lastHeapHandle)
        {
            Dictionary<Address, NativeAllocation> ret;

            if (!heaps.TryGetValue(heapHandle, out ret))
            {
                ret = new Dictionary<Address, NativeAllocation>();
                heaps.Add(heapHandle, ret);
            }
            lastHeapHandle = heapHandle;
            lastHeapAllocs = ret;
            return ret;
        }

        public SymbolReader GetSymbolReader(string filePath, SymbolReaderOptions symbolFlags = SymbolReaderOptions.None)
        {
            return App.GetSymbolReader(filePath, symbolFlags);
        }

        private static EtwProfilerConfig CreateDefaultConfig()
        {
            var kernelKeywords = KernelTraceEventParser.Keywords.Default | KernelTraceEventParser.Keywords.VirtualAlloc | KernelTraceEventParser.Keywords.VAMap;

            return new EtwProfilerConfig(
                performExtraBenchmarksRun: true,
                kernelKeywords: kernelKeywords);
        }

        public string ShortName => "Native";
    }
}