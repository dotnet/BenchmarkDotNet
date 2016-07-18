using BenchmarkDotNet.Toolchains;
using System;

namespace BenchmarkDotNet.Jobs
{
    public class Job : IJob
    {
        public static readonly IJob Default = new Job();
        public static readonly IJob LegacyJitX86 = new Job { Platform = Platform.X86, Jit = Jit.LegacyJit };
        public static readonly IJob LegacyJitX64 = new Job { Platform = Platform.X64, Jit = Jit.LegacyJit };
        public static readonly IJob RyuJitX64 = new Job { Platform = Platform.X64, Jit = Jit.RyuJit };
        public static readonly IJob Dry = new Job { Mode = Mode.SingleRun, LaunchCount = 1, WarmupCount = 1, TargetCount = 1 };
        public static readonly IJob[] AllJits = { LegacyJitX86, LegacyJitX64, RyuJitX64 };
        public static readonly IJob Clr = new Job { Runtime = Runtime.Clr };
        public static readonly IJob Mono = new Job { Runtime = Runtime.Mono };
        public static readonly IJob MonoLlvm = new Job { Runtime = Runtime.Mono, Jit = Jit.Llvm};
        public static readonly IJob Core = new Job { Runtime = Runtime.Core };
        public static readonly IJob MediumRun = new Job { LaunchCount = 3, WarmupCount = 15, TargetCount = 100 };
        public static readonly IJob LongRun = new Job { LaunchCount = 3, WarmupCount = 30, TargetCount = 1000 };
        public static readonly IJob ConcurrentServerGC = new Job { GarbageCollection = new GarbageCollection { Server = true, Concurrent = true} };
        public static readonly IJob ConcurrentWorkstationGC = new Job { GarbageCollection = new GarbageCollection { Server = false, Concurrent = true } };

        public Mode Mode { get; set; } = Mode.Throughput;
        public Platform Platform { get; set; } = Platform.Host;
        public Jit Jit { get; set; } = Jit.Host;
        public IToolchain Toolchain { get; set; }
        public Runtime Runtime { get; set; } = Runtime.Host;
        public GarbageCollection GarbageCollection { get; set; } = GarbageCollection.Default;

        public Count LaunchCount { get; set; } = Count.Auto;
        public Count WarmupCount { get; set; } = Count.Auto;
        public Count TargetCount { get; set; } = Count.Auto;
        public Count IterationTime { get; set; } = Count.Auto;
        public Count Affinity { get; set; } = Count.Auto;

        public Property[] AllProperties => allProperties.Value;

        private Lazy<Property[]> allProperties { get; }

        public Job()
        {
            allProperties = new Lazy<Property[]>(this.GetAllProperties, isThreadSafe: true);
        }

        public bool Equals(IJob other)
        {
            var ownProperties = AllProperties;
            var otherProperties = other.AllProperties;

            if (ownProperties.Length != otherProperties.Length)
            {
                return false;
            }

            for (int i = 0; i < ownProperties.Length; i++)
            {
                if (!ownProperties[i].Equals(otherProperties[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}