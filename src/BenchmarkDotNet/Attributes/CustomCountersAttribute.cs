using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies custom hardware counters to be collected during benchmarking.
    /// Use this when the predefined HardwareCounter enum values don't match the counters 
    /// available on your machine (e.g., AMD-specific counters like DcacheMisses, IcacheMisses).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class CustomCountersAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        protected CustomCountersAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        /// <summary>
        /// Creates a CustomCountersAttribute with the specified counter names.
        /// Counter names must match the ETW profile source names available on the machine.
        /// Use TraceEventProfileSources.GetInfo().Keys to discover available counters.
        /// </summary>
        /// <param name="counterNames">The ETW profile source names (e.g., "DcacheMisses", "IcacheMisses")</param>
        public CustomCountersAttribute(params string[] counterNames)
        {
            var config = ManualConfig.CreateEmpty();
            foreach (var name in counterNames)
            {
                config.AddCustomCounters(new CustomCounter(name));
            }
            Config = config;
        }
        public IConfig Config { get; }
    }
}