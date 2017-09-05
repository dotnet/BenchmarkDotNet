using System;
using System.Reflection;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines
{
    public struct GcStats
    {
        internal const string ResultsLinePrefix = "GC: ";

        public static readonly long AllocationQuantum = CalculateAllocationQuantumSize(); 

        private static readonly Func<long> GetAllocatedBytesForCurrentThreadDelegate = GetAllocatedBytesForCurrentThread();

        public static readonly GcStats Empty = new GcStats(0, 0, 0, 0, 0);

        private GcStats(int gen0Collections, int gen1Collections, int gen2Collections, long allocatedBytes, long totalOperations)
        {
            Gen0Collections = gen0Collections;
            Gen1Collections = gen1Collections;
            Gen2Collections = gen2Collections;
            AllocatedBytes = allocatedBytes;
            TotalOperations = totalOperations;
        }

        // did not use array here just to avoid heap allocation
        public int Gen0Collections { get; }
        public int Gen1Collections { get; }
        public int Gen2Collections { get; }

        /// <summary>
        /// Total per all runs
        /// </summary>
        private long AllocatedBytes { get; }

        public long TotalOperations { get; }

        public long BytesAllocatedPerOperation 
            => GetTotalAllocatedBytes(true) == 0 
                ? 0 
                : (long)Math.Round( // let's round it to reduce the side effects of Allocation quantum
                    (double)GetTotalAllocatedBytes(true) / TotalOperations, 
                    MidpointRounding.ToEven); 

        public static GcStats operator +(GcStats left, GcStats right)
        {
            return new GcStats(
                left.Gen0Collections + right.Gen0Collections,
                left.Gen1Collections + right.Gen1Collections,
                left.Gen2Collections + right.Gen2Collections,
                left.AllocatedBytes + right.AllocatedBytes,
                left.TotalOperations + right.TotalOperations);
        }

        public static GcStats operator -(GcStats left, GcStats right)
        {
            return new GcStats(
                Math.Max(0, left.Gen0Collections - right.Gen0Collections),
                Math.Max(0, left.Gen1Collections - right.Gen1Collections),
                Math.Max(0, left.Gen2Collections - right.Gen2Collections),
                Math.Max(0, left.AllocatedBytes - right.AllocatedBytes),
                Math.Max(0, left.TotalOperations - right.TotalOperations));
        }

        public GcStats WithTotalOperations(long totalOperationsCount)
            => this + new GcStats(0, 0, 0, 0, totalOperationsCount);

        public int GetCollectionsCount(int generation)
        {
            if (generation == 0)
                return Gen0Collections;
            if (generation == 1)
                return Gen1Collections;

            return Gen2Collections;
        }

        /// <summary>
        /// returns total allocated bytes (not per operation)
        /// </summary>
        /// <param name="excludeAllocationQuantumSideEffects">Allocation quantum can affecting some of our nano-benchmarks in non-deterministic way.
        /// when this parameter is set to true and the number of all allocated bytes is less or equal AQ, we ignore AQ and put 0 to the results</param>
        /// <returns></returns>
        public long GetTotalAllocatedBytes(bool excludeAllocationQuantumSideEffects)
        {
            if (!excludeAllocationQuantumSideEffects)
                return AllocatedBytes;

            return AllocatedBytes <= AllocationQuantum ? 0L : AllocatedBytes;
        }

        public static GcStats ReadInitial(bool isDiagnosticsEnabled)
        {
            // this will force GC.Collect, so we want to do this before collecting collections counts
            long allocatedBytes = GetAllocatedBytes(isDiagnosticsEnabled); 

            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),
                allocatedBytes,
                0);
        }

        public static GcStats ReadFinal(bool isDiagnosticsEnabled)
        {
            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),

                // this will force GC.Collect, so we want to do this after collecting collections counts 
                // to exclude this single full forced collection from results
                GetAllocatedBytes(isDiagnosticsEnabled), 
                0);
        }

        public static GcStats FromForced(int forcedFullGarbageCollections)
            => new GcStats(forcedFullGarbageCollections, forcedFullGarbageCollections, forcedFullGarbageCollections, 0, 0);

        private static long GetAllocatedBytes(bool isDiagnosticsEnabled)
        {
            if (!isDiagnosticsEnabled 
                || RuntimeInformation.IsMono()) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-
                return 0;

            // "This instance Int64 property returns the number of bytes that have been allocated by a specific 
            // AppDomain. The number is accurate as of the last garbage collection." - CLR via C#
            // so we enforce GC.Collect here just to make sure we get accurate results
            GC.Collect();
#if NETCOREAPP1_1
            return GetAllocatedBytesForCurrentThreadDelegate.Invoke();
#elif NETCOREAPP2_0
            return GC.GetAllocatedBytesForCurrentThread();
#elif CLASSIC
            return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
#endif
        }

        private static Func<long> GetAllocatedBytesForCurrentThread()
        {
            // for some versions of .NET Core this method is internal, 
            // for some public and for others public and exposed ;)
            var method = typeof(GC).GetTypeInfo().GetMethod("GetAllocatedBytesForCurrentThread",
                            BindingFlags.Public | BindingFlags.Static)
                      ?? typeof(GC).GetTypeInfo().GetMethod("GetAllocatedBytesForCurrentThread",
                            BindingFlags.NonPublic | BindingFlags.Static);

            return () => (long)method.Invoke(null, null);
        }

        public string ToOutputLine() 
            => $"{ResultsLinePrefix} {Gen0Collections} {Gen1Collections} {Gen2Collections} {AllocatedBytes} {TotalOperations}";

        public static GcStats Parse(string line)
        {
            if(!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            int gen0, gen1, gen2;
            long allocatedBytes, totalOperationsCount;
            var measurementSplit = line.Remove(0, ResultsLinePrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(measurementSplit[0], out gen0)
                || !int.TryParse(measurementSplit[1], out gen1)
                || !int.TryParse(measurementSplit[2], out gen2)
                || !long.TryParse(measurementSplit[3], out allocatedBytes)
                || !long.TryParse(measurementSplit[4], out totalOperationsCount))
            {
                throw new NotSupportedException("Invalid string");
            }

            return new GcStats(gen0, gen1, gen2, allocatedBytes, totalOperationsCount);
        }

        public override string ToString() => ToOutputLine();

        /// <summary>
        /// code copied from https://github.com/rsdn/CodeJam/blob/71a6542b6e5c52ea8dd92c601adad11e62796a98/PerfTests/src/%5BL4_Configuration%5D/Metrics/%5BMetricValuesProvider%5D/GcMetricValuesProvider.cs#L63-L89
        /// </summary>
        /// <returns></returns>
        private static long CalculateAllocationQuantumSize()
        {
            long result;
            int retry = 0;
            do
            {
                if (++retry > 10)
                {
                    result = 8192; // 8kb https://github.com/dotnet/coreclr/blob/master/Documentation/botr/garbage-collection.md
                    break;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                result = GC.GetTotalMemory(false);
                var tmp = new object();
                result = GC.GetTotalMemory(false) - result;
                GC.KeepAlive(tmp);
            } while (result <= 0);

            return result;
        }
    }
}