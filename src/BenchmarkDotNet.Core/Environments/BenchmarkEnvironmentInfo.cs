using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Environments
{
    public class BenchmarkEnvironmentInfo
    {
        internal const string RuntimeInfoPrefix = "Runtime=";
        internal const string GcInfoPrefix = "GC=";

        public string Architecture { get; protected set; }

        public string Configuration { get; protected set; }

        public string RuntimeVersion { get; protected set; }

        public bool HasAttachedDebugger { get; protected set; }

        public bool HasRyuJit { get; protected set; }

        public string JitInfo { get; protected set; }

        public bool IsServerGC { get; protected set; }

        public bool IsConcurrentGC { get; protected set; }

        public long GCAllocationQuantum { get; protected set; }

        protected BenchmarkEnvironmentInfo()
        {
            Architecture = RuntimeInformation.GetArchitecture();
            RuntimeVersion = RuntimeInformation.GetRuntimeVersion();
            Configuration = RuntimeInformation.GetConfiguration();
            HasRyuJit = RuntimeInformation.HasRyuJit();
            JitInfo = RuntimeInformation.GetJitInfo();
            IsServerGC = GCSettings.IsServerGC;
            IsConcurrentGC = GCSettings.LatencyMode != GCLatencyMode.Batch;
            HasAttachedDebugger = Debugger.IsAttached;
            GCAllocationQuantum = GcStats.AllocationQuantum;
        }

        public static BenchmarkEnvironmentInfo GetCurrent() => new BenchmarkEnvironmentInfo();

        // ReSharper disable once UnusedMemberInSuper.Global
        public virtual IEnumerable<string> ToFormattedString()
        {
            yield return "Benchmark Process Environment Information:";
            yield return $"{RuntimeInfoPrefix}{GetRuntimeInfo()}";
            yield return $"{GcInfoPrefix}{GetGcConcurrentFlag()} {GetGcServerFlag()}";
        }

        protected string GetConfigurationFlag() => Configuration == RuntimeInformation.Unknown || Configuration == RuntimeInformation.ReleaseConfigurationName
            ? ""
            : Configuration;

        protected string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        protected string GetGcServerFlag() => IsServerGC ? "Server" : "Workstation";

        protected string GetGcConcurrentFlag() => IsConcurrentGC ? "Concurrent" : "Non-concurrent";

        internal string GetRuntimeInfo()
        {
            string jitInfo = string.Join(" ", new[] { JitInfo, GetConfigurationFlag(), GetDebuggerFlag() }.Where(title => title != ""));
            return $"{RuntimeVersion}, {Architecture} {jitInfo}";
        }
    }
}