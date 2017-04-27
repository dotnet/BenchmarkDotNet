using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using BenchmarkDotNet.Portability;

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

        protected BenchmarkEnvironmentInfo(RuntimeInformation runtimeInformation)
        {
            Architecture = runtimeInformation.Architecture;
            RuntimeVersion = runtimeInformation.GetRuntimeVersion();
            Configuration = runtimeInformation.GetConfiguration();
            HasRyuJit = runtimeInformation.HasRyuJit;
            JitInfo = runtimeInformation.JitInfo;
            IsServerGC = GCSettings.IsServerGC;
            IsConcurrentGC = GCSettings.LatencyMode != GCLatencyMode.Batch;
            HasAttachedDebugger = Debugger.IsAttached;
        }

        public static BenchmarkEnvironmentInfo GetCurrent(RuntimeInformation runtimeInformation) => new BenchmarkEnvironmentInfo(runtimeInformation);

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

        internal string GetRuntimeInfo() => $"{RuntimeVersion}, {Architecture} {JitInfo}{GetConfigurationFlag()}{GetDebuggerFlag()}";
    }
}