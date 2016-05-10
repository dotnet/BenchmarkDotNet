using System;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class BenchmarkTestExecutor
    {
        /// <summary>
        /// Runs Benchmarks with the most simple config (SingleRunFastConfig) 
        /// combined with any benchmark config applied to TBenchmark (via an attribute)
        /// By default will verify if every benchmark was successfully executed
        /// </summary>
        /// <typeparam name="TBenchmark">type that defines Benchmarks</typeparam>
        /// <param name="config">Optional custom config to be used instead of the default</param>
        /// <param name="fullValidation">Optional: disable validation (default = true/enabled)</param>
        /// <returns>The summary from the benchmark run</returns>
        internal static Reports.Summary CanExecute<TBenchmark>(IConfig config = null, bool fullValidation = true)
        {
            return CanExecute(typeof(TBenchmark), config, fullValidation);
        }

        /// <summary>
        /// Runs Benchmarks with the most simple config (SingleRunFastConfig) 
        /// combined with any benchmark config applied to Type (via an attribute)
        /// By default will verify if every benchmark was successfully executed
        /// </summary>
        /// <param name="type">type that defines Benchmarks</param>
        /// <param name="config">Optional custom config to be used instead of the default</param>
        /// <param name="fullValidation">Optional: disable validation (default = true/enabled)</param>
        /// <returns>The summary from the benchmark run</returns>
        internal static Reports.Summary CanExecute(Type type, IConfig config = null, bool fullValidation = true)
        {
            // Add logging, so the Benchmark execution is in the TestRunner output (makes Debugging easier)
            if (config == null)
            {
                config = new SingleRunFastConfig()
                             .With(DefaultConfig.Instance.GetLoggers().ToArray())
                             .With(DefaultConfig.Instance.GetColumns().ToArray());
            }

            // Make sure we ALWAYS conbine the Config (default or passed in) with any Config applied to the Type/Class
            var summary = BenchmarkRunner.Run(type, BenchmarkConverter.GetFullConfig(type, config));

            if (fullValidation)
            {
                Assert.False(summary.HasCriticalValidationErrors, "The \"Summary\" should have NOT \"HasCriticalValidationErrors\"");
                Assert.True(summary.Reports.Any(), "The \"Summary\" should contain at least one \"BenchmarkReport\" in the \"Reports\" collection");
                Assert.True(summary.Reports.All(r => r.ExecuteResults.Any(er => er.FoundExecutable && er.Data.Any())),
                            "All reports should have at least one \"ExecuteResult\" with \"FoundExecutable\" = true and at least one \"Data\" item");
                Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()),
                            "All reports should have at least one \"Measurment\" in the \"AllMeasurements\" collection");
            }

            return summary;
        }
    }
}