using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Diagnosers
{
    public class PmcStats
    {
        public long TotalOperations { get; set; }
        public IReadOnlyDictionary<HardwareCounterInfo, PreciseMachineCounter> Counters { get; }
        private IReadOnlyDictionary<int, PreciseMachineCounter> CountersByProfileSourceId { get; }

        public PmcStats() { throw new InvalidOperationException("should never be used"); }

        public PmcStats(IReadOnlyCollection<HardwareCounterInfo> hardwareCounters, Func<HardwareCounterInfo, PreciseMachineCounter> factory)
        {
            CountersByProfileSourceId = hardwareCounters
                .Select(factory)
                .ToDictionary
                (
                    counter => counter.ProfileSourceId,
                    counter => counter
                );
            Counters = CountersByProfileSourceId.ToDictionary(c => c.Value.Counter, c => c.Value);
        }

        internal void Handle(int profileSourceId, ulong instructionPointer)
        {
            if (CountersByProfileSourceId.TryGetValue(profileSourceId, out var counter))
                counter.OnSample(instructionPointer);
        }
    }
}