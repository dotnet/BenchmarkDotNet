﻿using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines
{
    public struct GcStats
    {
        internal const string ResultsLinePrefix = "GC: ";

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
        public long AllocatedBytes { get; }
        public long TotalOperations { get; }

        public long BytesAllocatedPerOperation 
            => (long)Math.Round( // let's round it to reduce the side effects of Allocation quantum
                (double)AllocatedBytes / TotalOperations, 
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
            if (!isDiagnosticsEnabled)
                return 0;

            // "This instance Int64 property returns the number of bytes that have been allocated by a specific 
            // AppDomain. The number is accurate as of the last garbage collection." - CLR via C# 
            // about AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize which is used in Classic implementation
            // so we enforce GC.Collect here just to make sure we get accurate results
            GC.Collect();

            return ServicesProvider.ResourcesService.GetAllocatedBytes();
        }

        public string ToOutputLine() 
            => $"{ResultsLinePrefix} {Gen0Collections} {Gen1Collections} {Gen2Collections} {AllocatedBytes} {TotalOperations}";

        public static GcStats Parse(string line)
        {
            if(!line.StartsWith(ResultsLinePrefix))
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
    }
}