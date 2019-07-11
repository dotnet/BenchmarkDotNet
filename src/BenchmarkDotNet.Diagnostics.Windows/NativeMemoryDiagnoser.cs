using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Address = System.UInt64;

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

        //Code is inspired by https://github.com/Microsoft/perfview/blob/master/src/PerfView/PerfViewData.cs#L5719-L5944
        private (long totalMemory, long memoryLeak) ParseEtlFile(BenchmarkCase parameters)
        {
            (long totalMemory, long memoryLeak) result = (0, 0);

            var traceFilePath = etwProfiler.BenchmarkToEtlFile[parameters];

            using (var eventLog = new TraceLog(TraceLog.CreateFromEventTraceLogFile(traceFilePath)))
            {
                TraceEvents events = eventLog.Events;

                var eventSource = events.GetSource();

                var bdnEventsParser = new EngineEventLogParser(eventSource);

                var start = false;
                var isFirstActualStartEnd = false;

                long totalOperation = 0;

                bdnEventsParser.WorkloadActualStart += data =>
                {
                    if (!isFirstActualStartEnd)
                    {
                        start = true;
                    }

                    totalOperation = data.TotalOperations;
                };
                bdnEventsParser.WorkloadActualStop += data =>
                {
                    start = false;
                    isFirstActualStartEnd = true;
                };

                var heapParser = new HeapTraceProviderTraceEventParser(eventSource);
                // We index by heap address and then within the heap we remember the allocation stack
                var heaps = new Dictionary<Address, Dictionary<Address, long>>();
                Dictionary<Address, long> lastHeapAllocs = null;

                Address lastHeapHandle = 0;

                long nativeLeakSize = 0;
                long totalAllocation = 0;

                heapParser.HeapTraceAlloc += delegate (HeapAllocTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }

                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                        allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);

                    allocs[data.AllocAddress] = data.AllocSize;

                    checked
                    {
                        nativeLeakSize += data.AllocSize;
                        totalAllocation += data.AllocSize;
                    }
                };
                heapParser.HeapTraceFree += delegate (HeapFreeTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }
                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    if (allocs.TryGetValue(data.FreeAddress, out long alloc))
                    {
                        nativeLeakSize -= alloc;

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

                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    // This is a clone of the Free code
                    if (allocs.TryGetValue(data.OldAllocAddress, out long alloc))
                    {
                        nativeLeakSize -= alloc;

                        allocs.Remove(data.OldAllocAddress);
                    }

                    // This is a clone of the Alloc code (sigh don't clone code)
                    allocs[data.NewAllocAddress] = data.NewAllocSize;

                    nativeLeakSize += data.NewAllocSize;
                };
                heapParser.HeapTraceDestroy += delegate (HeapTraceData data)
                {
                    if (!start)
                    {
                        return;
                    }

                    // Heap is dieing, kill all objects in it.
                    var allocs = lastHeapAllocs;
                    if (data.HeapHandle != lastHeapHandle)
                    {
                        allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                    }

                    foreach (var alloc in allocs.Values)
                    {
                        // TODO this is a clone of the free code.
                        nativeLeakSize -= alloc;
                    }
                };

                eventSource.Process();

                Logger.WriteLine();
                Logger.WriteLineHeader(LogSeparator);
                Logger.WriteLineInfo($"{parameters.DisplayInfo}");
                Logger.WriteLineHeader(LogSeparator);

                result.totalMemory = totalAllocation / totalOperation;
                result.memoryLeak = nativeLeakSize / totalOperation;

                Logger.WriteLine($"Total allocated native memory: {result.totalMemory:n3}");
                if (nativeLeakSize != 0)
                {
                    Logger.WriteLine($"Total memory leak: {result.memoryLeak:n3}");
                }
                var heapInfoList = heaps.Select(h => new { Address = h.Key, h.Value.Count, types = h.Value.Values });
                foreach (var item in heapInfoList.Where(p => p.Count > 0))
                {
                    Logger.WriteLine($"Count of not deallocated object: {item.Count / totalOperation}");
                }
            }

            return result;
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => etwProfiler.GetRunMode(benchmarkCase);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!etwProfiler.BenchmarkToEtlFile.TryGetValue(results.BenchmarkCase, out var traceFilePath))
                yield break;

            var result = ParseEtlFile(results.BenchmarkCase);

            yield return new Metric(new AllocatedNativeMemoryDescriptor(), result.totalMemory);
            yield return new Metric(new NativeMemoryLeakDescriptor(), result.memoryLeak);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => etwProfiler.Validate(validationParameters);

        /// <summary>
        /// Implements a simple one-element cache for find the heap to look in.
        /// </summary>
        private static Dictionary<Address, long> CreateHeapCache(Address heapHandle, Dictionary<Address, Dictionary<Address, long>> heaps, ref Dictionary<Address, long> lastHeapAllocs, ref Address lastHeapHandle)
        {
            Dictionary<Address, long> ret;

            if (!heaps.TryGetValue(heapHandle, out ret))
            {
                ret = new Dictionary<Address, long>();
                heaps.Add(heapHandle, ret);
            }
            lastHeapHandle = heapHandle;
            lastHeapAllocs = ret;
            return ret;
        }

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