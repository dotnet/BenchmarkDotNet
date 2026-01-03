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

            CountersByProfileSourceId = hwCounters
                .Concat(customCountersDict.Where(kv => !hwCounters.ContainsKey(kv.Key)))
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