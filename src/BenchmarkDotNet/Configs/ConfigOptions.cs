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
        DisableOptimizationsValidator = 1 << 3,
        /// <summary>
        /// determines if the exported result files should not be overwritten (be default they are overwritten)
        /// </summary>
        DontOverwriteResults = 1 << 4,
        /// <summary>
        /// Determines if the log file should be disabled.
        /// </summary>
        DisableLogFile = 1 << 5,
        /// <summary>
        /// Determines whether build output should be logged.
        /// </summary>
        LogBuildOutput = 1 << 6,
        /// <summary>
        /// Determines whether to generate msbuild binlogs
        /// </summary>
        GenerateMSBuildBinLog = 1 << 7,
        /// <summary>
        /// Performs apples-to-apples comparison for provided benchmarks and jobs. Experimental, will change in the near future!
        /// </summary>
        ApplesToApples = 1 << 8,
        /// <summary>
        /// Continue the execution if the last run was stopped.
        /// </summary>
        Resume = 1 << 9
    }

    internal static class ConfigOptionsExtensions
    {
        internal static bool IsSet(this ConfigOptions currentValue, ConfigOptions flag) => (currentValue & flag) == flag;

        internal static ConfigOptions Set(this ConfigOptions currentValue, bool value, ConfigOptions flag) => value ? currentValue | flag : currentValue & ~flag;
    }
}
