using System;

namespace BenchmarkDotNet.Configs
{
    [Flags]
    public enum ConfigOptions
    {
        /// <summary>
        /// no custom settings
        /// </summary>
        Default = 0,
        /// <summary>
        /// determines if all auto-generated files should be kept after running the benchmarks (be default they are removed)
        /// </summary>
        KeepBenchmarkFiles = 1 << 0,
        /// <summary>
        /// determines if all benchmarks results should be joined into a single summary (by default we have a summary per type)
        /// </summary>
        JoinSummary = 1 << 1,
        /// <summary>
        /// determines if benchmarking should be stopped after the first error (by default it's not)
        /// </summary>
        StopOnFirstError = 1 << 2,
        /// <summary>
        /// determines if "mandatory" optimizations validator should be entirely turned off
        /// </summary>
        DisableOptimizationsValidator = 1 << 3
    }

    internal static class ConfigOptionsExtensions
    {
        internal static bool IsSet(this ConfigOptions currentValue, ConfigOptions flag) => (currentValue & flag) == flag;

        internal static ConfigOptions Set(this ConfigOptions currentValue, bool value, ConfigOptions flag) => value ? currentValue | flag : currentValue & ~flag;
    }
}