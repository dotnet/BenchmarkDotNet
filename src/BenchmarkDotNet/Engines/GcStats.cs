using System;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public struct GcStats : IEquatable<GcStats>
    {
        internal const string ResultsLinePrefix = "// GC: ";

        public static readonly long AllocationQuantum = CalculateAllocationQuantumSize();

        public static readonly GcStats Empty = default;

        private GcStats(int gen0Collections, int gen1Collections, int gen2Collections, long? allocatedBytes, long totalOperations)
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
        private long? AllocatedBytes { get; }

        public long TotalOperations { get; }

        public long? GetBytesAllocatedPerOperation(BenchmarkCase benchmarkCase)
        {
            bool excludeAllocationQuantumSideEffects = benchmarkCase.GetRuntime().RuntimeMoniker <= RuntimeMoniker.NetCoreApp20; // the issue got fixed for .NET Core 2.0+ https://github.com/dotnet/coreclr/issues/10207

            long? allocatedBytes = GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects);
            return allocatedBytes == null ? null
                : allocatedBytes == 0 ? 0
                : (long) Math.Round( // let's round it to reduce the side effects of Allocation quantum
                    (double) allocatedBytes.Value / TotalOperations,
                    MidpointRounding.ToEven);
        }

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
                ClampToPositive(left.AllocatedBytes - right.AllocatedBytes),
                Math.Max(0, left.TotalOperations - right.TotalOperations));
        }

        private static long? ClampToPositive(long? num)
        {
            return num.HasValue ? Math.Max(0, num.Value) : null;
        }

        public GcStats WithTotalOperations(long totalOperationsCount)
            => this + new GcStats(0, 0, 0, 0, totalOperationsCount);

        public int GetCollectionsCount(int generation)
        {
            switch (generation) {
                case 0:
                    return Gen0Collections;
                case 1:
                    return Gen1Collections;
                default:
                    return Gen2Collections;
            }
        }

        /// <summary>
        /// returns total allocated bytes (not per operation)
        /// </summary>
        /// <param name="excludeAllocationQuantumSideEffects">Allocation quantum can affecting some of our nano-benchmarks in non-deterministic way.
        /// when this parameter is set to true and the number of all allocated bytes is less or equal AQ, we ignore AQ and put 0 to the results</param>
        /// <returns></returns>
        public long? GetTotalAllocatedBytes(bool excludeAllocationQuantumSideEffects)
        {
            if (AllocatedBytes == null)
                return null;

            if (!excludeAllocationQuantumSideEffects)
                return AllocatedBytes;

            return AllocatedBytes <= AllocationQuantum ? 0L : AllocatedBytes;
        }

        public static GcStats ReadInitial()
        {
            // this will force GC.Collect, so we want to do this before collecting collections counts
            long? allocatedBytes = GetAllocatedBytes();

            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),
                allocatedBytes,
                0);
        }

        public static GcStats ReadFinal()
        {
            return new GcStats(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),

                // this will force GC.Collect, so we want to do this after collecting collections counts
                // to exclude this single full forced collection from results
                GetAllocatedBytes(),
                0);
        }

        [PublicAPI]
        public static GcStats FromForced(int forcedFullGarbageCollections)
            => new GcStats(forcedFullGarbageCollections, forcedFullGarbageCollections, forcedFullGarbageCollections, 0, 0);

        private static long? GetAllocatedBytes()
        {
            // we have no tests for WASM and don't want to risk introducing a new bug (https://github.com/dotnet/BenchmarkDotNet/issues/2226)
            if (RuntimeInformation.IsWasm)
                return null;

            // "This instance Int64 property returns the number of bytes that have been allocated by a specific
            // AppDomain. The number is accurate as of the last garbage collection." - CLR via C#
            // so we enforce GC.Collect here just to make sure we get accurate results
            GC.Collect();

#if NET6_0_OR_GREATER
            return GC.GetTotalAllocatedBytes(precise: true);
#else
            if (GcHelpers.GetTotalAllocatedBytesDelegate != null) // it's .NET Core 3.0 with the new API available
                return GcHelpers.GetTotalAllocatedBytesDelegate.Invoke(true); // true for the "precise" argument

            if (GcHelpers.CanUseMonitoringTotalAllocatedMemorySize) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-
                return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

            if (GcHelpers.GetAllocatedBytesForCurrentThreadDelegate != null)
                return GcHelpers.GetAllocatedBytesForCurrentThreadDelegate.Invoke();

            return null;
#endif
        }

        public string ToOutputLine()
            => $"{ResultsLinePrefix} {Gen0Collections} {Gen1Collections} {Gen2Collections} {AllocatedBytes?.ToString() ?? MetricColumn.UnknownRepresentation} {TotalOperations}";

        public static GcStats Parse(string line)
        {
            if (!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            var measurementSplit = line.Remove(0, ResultsLinePrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(measurementSplit[0], out int gen0)
                || !int.TryParse(measurementSplit[1], out int gen1)
                || !int.TryParse(measurementSplit[2], out int gen2)
                || !TryParse(measurementSplit[3], out long? allocatedBytes)
                || !long.TryParse(measurementSplit[4], out long totalOperationsCount))
            {
                throw new NotSupportedException("Invalid string");
            }

            return new GcStats(gen0, gen1, gen2, allocatedBytes, totalOperationsCount);
        }

        private static bool TryParse(string s, out long? result)
        {
            if (s == MetricColumn.UnknownRepresentation)
            {
                result = null;
                return true;
            }
            if (long.TryParse(s, out long r))
            {
                result = r;
                return true;
            }
            result = null;
            return false;
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

        public bool Equals(GcStats other) => Gen0Collections == other.Gen0Collections && Gen1Collections == other.Gen1Collections && Gen2Collections == other.Gen2Collections && AllocatedBytes == other.AllocatedBytes && TotalOperations == other.TotalOperations;

        public override bool Equals(object obj) => obj is GcStats other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Gen0Collections, Gen1Collections, Gen2Collections, AllocatedBytes, TotalOperations);

#if !NET6_0_OR_GREATER
        // Separate class to have the cctor run lazily, to avoid enabling monitoring before the benchmarks are ran.
        private static class GcHelpers
        {
            // do not reorder these, CheckMonitoringTotalAllocatedMemorySize relies on GetTotalAllocatedBytesDelegate being initialized first
            public static readonly Func<bool, long> GetTotalAllocatedBytesDelegate = CreateGetTotalAllocatedBytesDelegate();
            public static readonly Func<long> GetAllocatedBytesForCurrentThreadDelegate = CreateGetAllocatedBytesForCurrentThreadDelegate();
            public static readonly bool CanUseMonitoringTotalAllocatedMemorySize = CheckMonitoringTotalAllocatedMemorySize();

            private static Func<bool, long> CreateGetTotalAllocatedBytesDelegate()
            {
                try
                {
                    // this method is not a part of .NET Standard so we need to use reflection
                    var method = typeof(GC).GetTypeInfo().GetMethod("GetTotalAllocatedBytes", BindingFlags.Public | BindingFlags.Static);

                    if (method == null)
                        return null;

                    // we create delegate to avoid boxing, IMPORTANT!
                    var del = (Func<bool, long>)method.CreateDelegate(typeof(Func<bool, long>));

                    // verify the api works
                    return del.Invoke(true) >= 0 ? del : null;
                }
                catch
                {
                    return null;
                }
            }

            private static Func<long> CreateGetAllocatedBytesForCurrentThreadDelegate()
            {
                try
                {
                    // this method is not a part of .NET Standard so we need to use reflection
                    var method = typeof(GC).GetTypeInfo().GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static);

                    if (method == null)
                        return null;

                    // we create delegate to avoid boxing, IMPORTANT!
                    var del = (Func<long>)method.CreateDelegate(typeof(Func<long>));

                    // verify the api works
                    return del.Invoke() >= 0 ? del : null;
                }
                catch
                {
                    return null;
                }
            }

            private static bool CheckMonitoringTotalAllocatedMemorySize()
            {
                try
                {
                    // we potentially don't want to enable monitoring if we don't need it
                    if (GetTotalAllocatedBytesDelegate != null)
                        return false;

                    // check if monitoring is enabled
                    if (!AppDomain.MonitoringIsEnabled)
                        AppDomain.MonitoringIsEnabled = true;

                    // verify the api works
                    return AppDomain.MonitoringIsEnabled && AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize >= 0;
                }
                catch
                {
                    return false;
                }
            }
        }
#endif
    }
}
