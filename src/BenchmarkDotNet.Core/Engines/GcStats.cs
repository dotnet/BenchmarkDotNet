using System;

namespace BenchmarkDotNet.Engines
{
    public struct GcStats
    {
        internal const string ResultsLinePrefix = "GC: ";

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
        public long AllocatedBytes { get; }
        public long TotalOperations { get; }

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

        internal static GcStats ReadInitial(bool isDiagnosticsEnabled)
        {
            // this might force GC.Collect, so we want to do this before collecting collections counts
            long allocatedBytes = GetAllocatedBytes(isDiagnosticsEnabled); 

            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),
                allocatedBytes,
                0);
        }

        internal static GcStats ReadFinal(bool isDiagnosticsEnabled)
        {
            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),

                // this might force GC.Collect, so we want to do this after collecting collections counts 
                // to exclude this single full forced collection from results
                GetAllocatedBytes(isDiagnosticsEnabled), 
                0);
        }

        public static GcStats FromForced(int forcedFullGarbageCollections)
            => new GcStats(forcedFullGarbageCollections, forcedFullGarbageCollections, forcedFullGarbageCollections, 0, 0);

        private static long GetAllocatedBytes(bool isDiagnosticsEnabled)
        {
#if NETCOREAPP11 // when MS releases new version of .NET Runtime to nuget.org
            return GC.GetAllocatedBytesForCurrentThread(); // https://github.com/dotnet/corefx/pull/12489
#elif CLASSIC
            if (!isDiagnosticsEnabled)
                return 0;

            // "This instance Int64 property returns the number of bytes that have been allocated by a specific 
            // AppDomain. The number is accurate as of the last garbage collection." - CLR via C#
            // so we enforce GC.Collect here just to make sure we get accurate results
            GC.Collect();
            return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
#else
            return 0; // currently for .NET Core
#endif
        }

        internal string ToOutputLine() 
            => $"{ResultsLinePrefix} {Gen0Collections} {Gen1Collections} {Gen2Collections} {AllocatedBytes} {TotalOperations}";

        internal static GcStats Parse(string line)
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
    }
}