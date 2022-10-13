using System;
using System.Reflection;
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

        private static readonly Func<long> GetAllocatedBytesForCurrentThreadDelegate = CreateGetAllocatedBytesForCurrentThreadDelegate();
        private static readonly Func<bool, long> GetTotalAllocatedBytesDelegate = CreateGetTotalAllocatedBytesDelegate();

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

        public long GetBytesAllocatedPerOperation(BenchmarkCase benchmarkCase)
        {
            bool excludeAllocationQuantumSideEffects = benchmarkCase.GetRuntime().RuntimeMoniker <= RuntimeMoniker.NetCoreApp20; // the issue got fixed for .NET Core 2.0+ https://github.com/dotnet/coreclr/issues/10207

            return GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects) == 0
                ? 0
                : (long) Math.Round( // let's round it to reduce the side effects of Allocation quantum
                    (double) GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects) / TotalOperations,
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
                Math.Max(0, left.AllocatedBytes - right.AllocatedBytes),
                Math.Max(0, left.TotalOperations - right.TotalOperations));
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
        public long GetTotalAllocatedBytes(bool excludeAllocationQuantumSideEffects)
        {
            if (!excludeAllocationQuantumSideEffects)
                return AllocatedBytes;

            return AllocatedBytes <= AllocationQuantum ? 0L : AllocatedBytes;
        }

        public static GcStats ReadInitial()
        {
            // this will force GC.Collect, so we want to do this before collecting collections counts
            long allocatedBytes = GetAllocatedBytes();

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

        private static long GetAllocatedBytes()
        {
            if (RuntimeInformation.IsMono) // Monitoring is not available in Mono, see http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-
                return 0;

            // "This instance Int64 property returns the number of bytes that have been allocated by a specific
            // AppDomain. The number is accurate as of the last garbage collection." - CLR via C#
            // so we enforce GC.Collect here just to make sure we get accurate results
            GC.Collect();

            if (RuntimeInformation.IsFullFramework) // it can be a .NET app consuming our .NET Standard package
                return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

            if (GetTotalAllocatedBytesDelegate != null) // it's .NET Core 3.0 with the new API available
                return GetTotalAllocatedBytesDelegate.Invoke(true); // true for the "precise" argument

            // https://apisof.net/catalog/System.GC.GetAllocatedBytesForCurrentThread() is not part of the .NET Standard, so we use reflection to call it..
            return GetAllocatedBytesForCurrentThreadDelegate.Invoke();
        }

        private static Func<long> CreateGetAllocatedBytesForCurrentThreadDelegate()
        {
            // this method is not a part of .NET Standard so we need to use reflection
            var method = typeof(GC).GetTypeInfo().GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static);

            // we create delegate to avoid boxing, IMPORTANT!
            return method != null ? (Func<long>)method.CreateDelegate(typeof(Func<long>)) : null;
        }

        private static Func<bool, long> CreateGetTotalAllocatedBytesDelegate()
        {
            // this method is not a part of .NET Standard so we need to use reflection
            var method = typeof(GC).GetTypeInfo().GetMethod("GetTotalAllocatedBytes", BindingFlags.Public | BindingFlags.Static);

            // we create delegate to avoid boxing, IMPORTANT!
            return method != null ? (Func<bool, long>)method.CreateDelegate(typeof(Func<bool, long>)) : null;
        }

        public string ToOutputLine()
            => $"{ResultsLinePrefix} {Gen0Collections} {Gen1Collections} {Gen2Collections} {AllocatedBytes} {TotalOperations}";

        public static GcStats Parse(string line)
        {
            if (!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            var measurementSplit = line.Remove(0, ResultsLinePrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(measurementSplit[0], out int gen0)
                || !int.TryParse(measurementSplit[1], out int gen1)
                || !int.TryParse(measurementSplit[2], out int gen2)
                || !long.TryParse(measurementSplit[3], out long allocatedBytes)
                || !long.TryParse(measurementSplit[4], out long totalOperationsCount))
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

        public bool Equals(GcStats other) => Gen0Collections == other.Gen0Collections && Gen1Collections == other.Gen1Collections && Gen2Collections == other.Gen2Collections && AllocatedBytes == other.AllocatedBytes && TotalOperations == other.TotalOperations;

        public override bool Equals(object obj) => obj is GcStats other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Gen0Collections;
                hashCode = (hashCode * 397) ^ Gen1Collections;
                hashCode = (hashCode * 397) ^ Gen2Collections;
                hashCode = (hashCode * 397) ^ AllocatedBytes.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalOperations.GetHashCode();
                return hashCode;
            }
        }
    }
}