using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Environments
{
    public class BenchmarkEnvironmentInfo
    {
        internal const string RuntimeInfoPrefix = "Runtime=";
        internal const string GcInfoPrefix = "GC=";
        internal const string HardwareIntrinsicsPrefix = "HardwareIntrinsics=";

        [PublicAPI] public string Architecture { get; protected set; }
        [PublicAPI] public string Configuration { get; protected set; }
        [PublicAPI] public string RuntimeVersion { get; protected set; }
        [PublicAPI] public bool HasAttachedDebugger { get; protected set; }
        [PublicAPI] public bool HasRyuJit { get; protected set; }
        [PublicAPI] public string JitInfo { get; protected set; }
        [PublicAPI] public string HardwareIntrinsicsShort { get; protected set; }
        [PublicAPI] public bool IsServerGC { get; protected set; }
        [PublicAPI] public bool IsConcurrentGC { get; protected set; }
        [PublicAPI] public long GCAllocationQuantum { get; protected set; }
        [PublicAPI] public bool InDocker { get; protected set; }

        protected BenchmarkEnvironmentInfo()
        {
            Architecture = RuntimeInformation.GetArchitecture();
            RuntimeVersion = RuntimeInformation.GetRuntimeVersion();
            Configuration = RuntimeInformation.GetConfiguration();
            HasRyuJit = RuntimeInformation.HasRyuJit();
            JitInfo = RuntimeInformation.GetJitInfo();
            HardwareIntrinsicsShort = HardwareIntrinsics.GetShortInfo();
            IsServerGC = GCSettings.IsServerGC;
            IsConcurrentGC = GCSettings.LatencyMode != GCLatencyMode.Batch;
            HasAttachedDebugger = Debugger.IsAttached;
            GCAllocationQuantum = GcStats.AllocationQuantum;
            InDocker = RuntimeInformation.IsRunningInContainer;
        }

        public static BenchmarkEnvironmentInfo GetCurrent() => new BenchmarkEnvironmentInfo();

        // ReSharper disable once UnusedMemberInSuper.Global
        public virtual IEnumerable<string> ToFormattedString()
        {
            yield return "Benchmark Process Environment Information:";
            yield return $"{RuntimeInfoPrefix}{GetRuntimeInfo()}";
            yield return $"{GcInfoPrefix}{GetGcConcurrentFlag()} {GetGcServerFlag()}";
            yield return $"{HardwareIntrinsicsPrefix}{HardwareIntrinsics.GetFullInfo(RuntimeInformation.GetCurrentPlatform())} {HardwareIntrinsics.GetVectorSize()}";
        }

        [PublicAPI] protected string GetConfigurationFlag() => Configuration == RuntimeInformation.Unknown || Configuration == RuntimeInformation.ReleaseConfigurationName
            ? ""
            : Configuration;

        [PublicAPI] protected string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";
        [PublicAPI] protected string GetGcServerFlag() => IsServerGC ? "Server" : "Workstation";
        [PublicAPI] protected string GetGcConcurrentFlag() => IsConcurrentGC ? "Concurrent" : "Non-concurrent";

        internal string GetRuntimeInfo()
        {
            string jitInfo = string.Join(" ", new[] { JitInfo, HardwareIntrinsicsShort, GetConfigurationFlag(), GetDebuggerFlag() }.Where(title => title != ""));
            return $"{RuntimeVersion}, {Architecture} {jitInfo}";
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")] // TODO: should be used or removed
        public static IEnumerable<ValidationError> Validate(Job job)
        {
            if (job.Environment.Jit == Jit.RyuJit && !RuntimeInformation.HasRyuJit())
                yield return new ValidationError(true, "RyuJIT is requested but it is not available in current environment");
            var currentRuntime = RuntimeInformation.GetCurrentRuntime();
            if (job.Environment.Jit == Jit.LegacyJit && !(currentRuntime is ClrRuntime))
                yield return new ValidationError(true, $"LegacyJIT is requested but it is not available for {currentRuntime}");
        }
    }
}