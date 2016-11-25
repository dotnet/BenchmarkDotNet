using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal class MeasurementsPool
    {
        private readonly Stack<List<Measurement>> preAllocatedListsOfMeasurements;

        private MeasurementsPool(int capacity)
        {
            preAllocatedListsOfMeasurements = new Stack<List<Measurement>>(capacity);
        }

        internal List<Measurement> Next() => preAllocatedListsOfMeasurements.Pop();

        internal static MeasurementsPool PreAllocate(int capacity, int maxLength, int? configuredLength)
        {
            var pool = new MeasurementsPool(capacity);
            var maxSize = configuredLength.HasValue ? Math.Max(configuredLength.Value, maxLength) : maxLength;

            for (int i = 0; i < capacity; i++)
            {
                pool.preAllocatedListsOfMeasurements.Push(new List<Measurement>(maxSize));
            }

            return pool;
        }
    }
}