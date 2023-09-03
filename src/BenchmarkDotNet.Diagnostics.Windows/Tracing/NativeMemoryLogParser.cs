using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;
using Address = System.UInt64;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing
{
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias")]
    public class NativeMemoryLogParser
    {
        private static readonly string LogSeparator = new string('-', 20);

        private readonly string etlFilePath;

        private readonly BenchmarkCase benchmarkCase;

        private readonly ILogger logger;

        private readonly string moduleName;

        private readonly string[] functionNames;

        public NativeMemoryLogParser(string etlFilePath, BenchmarkCase benchmarkCase, ILogger logger,
            string programName)
        {
            this.etlFilePath = etlFilePath;
            this.benchmarkCase = benchmarkCase;
            this.logger = logger;

            moduleName = programName;
            functionNames = new[]
            {
                nameof(EngineParameters.WorkloadActionUnroll),
                nameof(EngineParameters.WorkloadActionNoUnroll)
            };
        }

        public IEnumerable<Metric> Parse()
        {
            var etlxFilePath = TraceLog.CreateFromEventTraceLogFile(etlFilePath);

            try
            {
                using (var traceLog = new TraceLog(etlxFilePath))
                {
                    return Parse(traceLog);
                }
            }
            finally
            {
                etlxFilePath.DeleteFileIfExists();
            }
        }

        //Code is inspired by https://github.com/Microsoft/perfview/blob/master/src/PerfView/PerfViewData.cs#L5719-L5944
        private IEnumerable<Metric> Parse(TraceLog traceLog)
        {
            var stackSource = new MutableTraceEventStackSource(traceLog);
            var eventSource = traceLog.Events.GetSource();

            var bdnEventsParser = new EngineEventLogParser(eventSource);

            var start = false;
            var isFirstActualStartEnd = false;

            long totalOperation = 0;
            long countOfAllocatedObject = 0;

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
            Dictionary<Address, long>? lastHeapAllocs = null;

            Address lastHeapHandle = 0;

            long nativeLeakSize = 0;
            long totalAllocation = 0;

            heapParser.HeapTraceAlloc += delegate(HeapAllocTraceData data)
            {
                if (!start)
                {
                    return;
                }

                var call = data.CallStackIndex();
                var frameIndex = stackSource.GetCallStack(call, data);

                if (!IsCallStackIn(frameIndex))
                {
                    return;
                }

                var allocs = lastHeapAllocs;
                if (data.HeapHandle != lastHeapHandle)
                {
                    allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                }

                allocs[data.AllocAddress] = data.AllocSize;

                checked
                {
                    countOfAllocatedObject++;
                    nativeLeakSize += data.AllocSize;
                    totalAllocation += data.AllocSize;
                }

                bool IsCallStackIn(StackSourceCallStackIndex index)
                {
                    while (index != StackSourceCallStackIndex.Invalid)
                    {
                        var frame = stackSource.GetFrameIndex(index);
                        var name = stackSource.GetFrameName(frame, false);

                        if (name.StartsWith(moduleName, StringComparison.Ordinal) &&
                            functionNames.Any(functionName => name.IndexOf(functionName, StringComparison.Ordinal) > 0))
                        {
                            return true;
                        }

                        index = stackSource.GetCallerIndex(index);
                    }

                    return false;
                }
            };

            heapParser.HeapTraceFree += delegate(HeapFreeTraceData data)
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

            heapParser.HeapTraceReAlloc += delegate(HeapReallocTraceData data)
            {
                if (!start)
                {
                    return;
                }
                // Reallocs that actually move stuff will cause a Alloc and delete event
                // so there is nothing to do for those events. But when the address is
                // the same we need to resize.
                if (data.OldAllocAddress != data.NewAllocAddress)
                {
                    return;
                }

                var allocs = lastHeapAllocs;
                if (data.HeapHandle != lastHeapHandle)
                {
                    allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                }

                if (allocs.TryGetValue(data.OldAllocAddress, out long alloc))
                {
                    // Free
                    nativeLeakSize -= alloc;

                    allocs.Remove(data.OldAllocAddress);

                    // Alloc
                    allocs[data.NewAllocAddress] = data.NewAllocSize;

                    checked
                    {
                        nativeLeakSize += data.NewAllocSize;
                    }
                }
            };

            heapParser.HeapTraceDestroy += delegate(HeapTraceData data)
            {
                if (!start)
                {
                    return;
                }

                // Heap is dying, kill all objects in it.
                var allocs = lastHeapAllocs;
                if (data.HeapHandle != lastHeapHandle)
                {
                    allocs = CreateHeapCache(data.HeapHandle, heaps, ref lastHeapAllocs, ref lastHeapHandle);
                }

                foreach (var alloc in allocs.Values)
                {
                    nativeLeakSize -= alloc;
                }
            };

            eventSource.Process();

            logger.WriteLine();
            logger.WriteLineHeader(LogSeparator);
            logger.WriteLineInfo($"{benchmarkCase.DisplayInfo}");
            logger.WriteLineHeader(LogSeparator);

            if (totalOperation == 0)
            {
                logger.WriteLine($"Something went wrong. The trace file {etlFilePath} does not contain BenchmarkDotNet engine events.");
                return Enumerable.Empty<Metric>();
            }

            var memoryAllocatedPerOperation = totalAllocation / totalOperation;
            var memoryLeakPerOperation = nativeLeakSize / totalOperation;

            logger.WriteLine($"Native memory allocated per single operation: {SizeValue.FromBytes(memoryAllocatedPerOperation).ToString(SizeUnit.B, benchmarkCase.Config.CultureInfo)}");
            logger.WriteLine($"Count of allocated object: {countOfAllocatedObject / totalOperation}");

            if (nativeLeakSize != 0)
            {
                logger.WriteLine($"Native memory leak per single operation: {SizeValue.FromBytes(memoryLeakPerOperation).ToString(SizeUnit.B, benchmarkCase.Config.CultureInfo)}");
            }

            var heapInfoList = heaps.Select(h => new { Address = h.Key, h.Value.Count, types = h.Value.Values });
            foreach (var item in heapInfoList.Where(p => p.Count > 0))
            {
                logger.WriteLine($"Count of not deallocated object: {item.Count / totalOperation}");
            }

            return new[]
            {
                new Metric(AllocatedNativeMemoryDescriptor.Instance, memoryAllocatedPerOperation),
                new Metric(NativeMemoryLeakDescriptor.Instance, memoryLeakPerOperation)
            };
        }

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
    }
}
