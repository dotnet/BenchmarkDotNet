using System.Linq;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Classic;

namespace BenchmarkDotNet.Jobs
{
    public class Job : IJob
    {
        public static readonly IJob Default = new Job();
        public static readonly IJob LegacyX86 = new Job { Platform = Platform.X86, Jit = Jit.LegacyJit };
        public static readonly IJob LegacyX64 = new Job { Platform = Platform.X64, Jit = Jit.LegacyJit };
        public static readonly IJob RyuJitX64 = new Job { Platform = Platform.X64, Jit = Jit.RyuJit };
        public static readonly IJob Dry = new Job { Mode = Mode.SingleRun, ProcessCount = 1, WarmupCount = 1, TargetCount = 1 };
        public static readonly IJob[] AllJits = { LegacyX86, LegacyX64, RyuJitX64 };
        public static readonly IJob Clr = new Job { Runtime = Runtime.Clr };
        public static readonly IJob Mono = new Job { Runtime = Runtime.Mono };

        public IToolchain Toolchain { get; set; } = ClassicToolchain.Instance;

        public Mode Mode { get; set; } = Mode.Throughput;
        public Platform Platform { get; set; } = Platform.Host;
        public Jit Jit { get; set; } = Jit.Host;
        public Framework Framework { get; set; } = Framework.Host;
        public Runtime Runtime { get; set; } = Runtime.Host;

        public Count ProcessCount { get; set; } = Count.Auto;
        public Count WarmupCount { get; set; } = Count.Auto;
        public Count TargetCount { get; set; } = Count.Auto;
        public Count IterationTime { get; set; } = Count.Auto;
        public Count Affinity { get; set; } = Count.Auto;

        public bool Equals(IJob other)
        {
            var ownProperties = this.GetAllProperties().ToArray();
            var otherProperties = other.GetAllProperties().ToArray();
            if (ownProperties.Length != otherProperties.Length)
                return false;
            int n = ownProperties.Length;
            return Enumerable.Range(0, n).All(i =>
                ownProperties[i].Key == otherProperties[i].Key &&
                ownProperties[i].Value == otherProperties[i].Value);
        }
    }
}