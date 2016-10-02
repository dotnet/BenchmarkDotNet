using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class BenchmarkEnvironmentInfo
    {
        internal const string RuntimeInfoPrefix = "Runtime=";

        public string Architecture { get; }

        public string Configuration { get; }

        public string RuntimeVersion { get; }

        public bool HasAttachedDebugger => Debugger.IsAttached;

        public bool HasRyuJit { get; }

        public bool IsServerGC { get; }

        public bool IsConcurrentGC { get; }

        protected BenchmarkEnvironmentInfo()
        {
            Architecture = RuntimeInformation.GetArchitecture();
            RuntimeVersion = RuntimeInformation.GetRuntimeVersion();
            Configuration = RuntimeInformation.GetConfiguration();
            HasRyuJit = RuntimeInformation.HasRyuJit();
            IsServerGC = GCSettings.IsServerGC;
            IsConcurrentGC = GCSettings.LatencyMode != GCLatencyMode.Batch;
        }

        public static BenchmarkEnvironmentInfo GetCurrent() => new BenchmarkEnvironmentInfo();

        // ReSharper disable once UnusedMemberInSuper.Global
        public virtual IEnumerable<string> ToFormattedString()
        {
            yield return "Benchmark Process Environment Information:";
            yield return $"{RuntimeInfoPrefix}{RuntimeVersion}, Arch={Architecture} {GetConfigurationFlag()}{GetDebuggerFlag()}{GetJitFlag()}";
            yield return $"GC={GetGcConcurrentFlag()} {GetGcServerFlag()}";
        }

        protected string GetJitFlag() => HasRyuJit ? " [RyuJIT]" : "";

        protected string GetConfigurationFlag() => Configuration == RuntimeInformation.Unknown ? "" : Configuration;

        protected string GetDebuggerFlag() => HasAttachedDebugger ? " [AttachedDebugger]" : "";

        protected string GetGcServerFlag() => IsServerGC ? "Server" : "Workstation";

        protected string GetGcConcurrentFlag() => IsConcurrentGC ? "Concurrent" : "Non-concurrent";
    }
}