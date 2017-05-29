using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkTestExecutor
    {
        protected readonly ITestOutputHelper Output;

        public BenchmarkTestExecutor()
        {
        }

        protected BenchmarkTestExecutor(ITestOutputHelper output)
        {
            Output = output;
        }

        /// <summary>
        /// Runs Benchmarks with the most simple config (SingleRunFastConfig) 
        /// combined with any benchmark config applied to TBenchmark (via an attribute)
        /// By default will verify if every benchmark was successfully executed
        /// </summary>
        /// <typeparam name="TBenchmark">type that defines Benchmarks</typeparam>
        /// <param name="config">Optional custom config to be used instead of the default</param>
        /// <param name="fullValidation">Optional: disable validation (default = true/enabled)</param>
        /// <returns>The summary from the benchmark run</returns>
        internal Reports.Summary CanExecute<TBenchmark>(IConfig config = null, bool fullValidation = true)
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
        protected Reports.Summary CanExecute(Type type, IConfig config = null, bool fullValidation = true)
        {
            // Add logging, so the Benchmark execution is in the TestRunner output (makes Debugging easier)
            if (config == null)
                config = CreateSimpleConfig();

            if (!config.GetLoggers().OfType<OutputLogger>().Any())
                config = config.With(Output != null ? new OutputLogger(Output) : ConsoleLogger.Default);

            if (!config.GetColumnProviders().Any())
                config = config.With(DefaultColumnProviders.Instance);

            // Make sure we ALWAYS combine the Config (default or passed in) with any Config applied to the Type/Class
            var summary = BenchmarkRunner.Run(type, BenchmarkConverter.GetFullConfig(type, config));

            if (fullValidation)
            {
                Assert.False(summary.HasCriticalValidationErrors, "The \"Summary\" should have NOT \"HasCriticalValidationErrors\"");

                Assert.True(summary.Reports.Any(), "The \"Summary\" should contain at least one \"BenchmarkReport\" in the \"Reports\" collection");

                Assert.True(summary.Reports.All(r => r.BuildResult.IsBuildSuccess),
                    "The following benchmarks are failed to build: " +
                    string.Join(", ", summary.Reports.Where(r => !r.BuildResult.IsBuildSuccess).Select(r => r.Benchmark.DisplayInfo)));

                Assert.True(summary.Reports.All(r => r.ExecuteResults != null),
                    "The following benchmarks don't have any execution results: " +
                    string.Join(", ", summary.Reports.Where(r => r.ExecuteResults == null).Select(r => r.Benchmark.DisplayInfo)));
                
                Assert.True(summary.Reports.All(r => r.ExecuteResults.Any(er => er.FoundExecutable && er.Data.Any())),
                    "All reports should have at least one \"ExecuteResult\" with \"FoundExecutable\" = true and at least one \"Data\" item");

                Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()),
                    "All reports should have at least one \"Measurement\" in the \"AllMeasurements\" collection");
            }

            return summary;
        }

        protected IConfig CreateSimpleConfig(OutputLogger logger = null, Job job = null)
        {
            var baseConfig = job == null ? (IConfig)new SingleRunFastConfig() : new SingleJobConfig(job);
            return baseConfig
                .With(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .With(DefaultColumnProviders.Instance);
        }
    }
}