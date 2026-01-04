using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Diagnosers
{
    public class PmcStats
    {
        public long TotalOperations { get; set; }
        public IReadOnlyDictionary<HardwareCounter, PreciseMachineCounter> Counters { get; }
        public IReadOnlyCollection<PreciseMachineCounter> CustomCounters { get; }
        private IReadOnlyDictionary<int, PreciseMachineCounter> CountersByProfileSourceId { get; }

        public PmcStats() { throw new InvalidOperationException("should never be used"); }

        public PmcStats(IReadOnlyCollection<HardwareCounter> hardwareCounters, Func<HardwareCounter, PreciseMachineCounter> factory)
            : this(hardwareCounters, Array.Empty<PreciseMachineCounter>(), factory)
        {
        }

        public PmcStats(IReadOnlyCollection<HardwareCounter> hardwareCounters, IReadOnlyCollection<PreciseMachineCounter> customCounters, Func<HardwareCounter, PreciseMachineCounter> factory)
        {
            var hwCounters = hardwareCounters
                .Select(factory)
                .ToDictionary(counter => counter.ProfileSourceId, counter => counter);

            var customCountersDict = customCounters.ToDictionary(counter => counter.ProfileSourceId, counter => counter);

            // Validate no ProfileSourceId collisions between hardware and custom counters
            var overlappingIds = customCountersDict.Keys
                .Where(hwCounters.ContainsKey)
                .ToArray();
            if (overlappingIds.Length > 0)
            {
                var collisions = overlappingIds
                    .Select(id => $"{id} ({hwCounters[id].Counter} / {customCountersDict[id].ShortName})")
                    .ToArray();
                throw new InvalidOperationException(
                    $"ProfileSourceId collision detected between hardware and custom counters. " +
                    $"Colliding counters: {string.Join(", ", collisions)}. " +
                    $"Remove either the hardware counter or the custom counter with the same profile source.");
            }

            CountersByProfileSourceId = hwCounters
                .Concat(customCountersDict)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            Counters = hwCounters.ToDictionary(c => c.Value.Counter, c => c.Value);
            CustomCounters = customCounters;
        }

        internal void Handle(int profileSourceId, ulong instructionPointer)
        {
            if (CountersByProfileSourceId.TryGetValue(profileSourceId, out var counter))
                counter.OnSample(instructionPointer);
        }
    }
}