using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkTestExecutor
    {
        protected readonly ITestOutputHelper Output;

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
        /// <param name="allowWarnings">Optional: don't fail the validation on logged warnings (default = false)</param>
        /// <returns>The summary from the benchmark run</returns>
        public Summary CanExecute<TBenchmark>(IConfig? config = null, bool fullValidation = true, bool allowWarnings = false)
            => CanExecute(typeof(TBenchmark), config, fullValidation, allowWarnings);

        /// <summary>
        /// Runs Benchmarks with the most simple config (SingleRunFastConfig)
        /// combined with any benchmark config applied to Type (via an attribute)
        /// By default will verify if every benchmark was successfully executed
        /// </summary>
        /// <param name="type">type that defines Benchmarks</param>
        /// <param name="config">Optional custom config to be used instead of the default</param>
        /// <param name="fullValidation">Optional: disable validation (default = true/enabled)</param>
        /// <param name="allowWarnings">Optional: don't fail the validation on logged warnings (default = false)</param>
        /// <returns>The summary from the benchmark run</returns>
        public Summary CanExecute(Type type, IConfig? config = null, bool fullValidation = true, bool allowWarnings = false)
        {
            // Make sure we ALWAYS combine the Config (default or passed in) with any Config applied to the Type/Class
            var warnings = new WarningCollectingLogger();
            var summary = BenchmarkRunner.Run(type, GetMinimalConfig(config).AddLogger(warnings));

            if (fullValidation)
            {
                ValidateSummary(summary, warnings, allowWarnings);
            }

            return summary;
        }

        public ValueTask<Summary> CanExecuteAsync<TBenchmark>(IConfig? config = null, bool fullValidation = true, bool allowWarnings = false)
            => CanExecuteAsync(typeof(TBenchmark), config, fullValidation, allowWarnings);

        public async ValueTask<Summary> CanExecuteAsync(Type type, IConfig? config = null, bool fullValidation = true, bool allowWarnings = false)
        {
            // Make sure we ALWAYS combine the Config (default or passed in) with any Config applied to the Type/Class
            var warnings = new WarningCollectingLogger();
            var summary = await BenchmarkRunner.RunAsync(type, GetMinimalConfig(config).AddLogger(warnings));

            if (fullValidation)
            {
                ValidateSummary(summary, warnings, allowWarnings);
            }

            return summary;
        }

        private IConfig GetMinimalConfig(IConfig? config)
        {
            // Add logging, so the Benchmark execution is in the TestRunner output (makes Debugging easier)
            if (config == null)
                config = CreateSimpleConfig();

            if (!config.GetLoggers().OfType<OutputLogger>().Any())
                config = config.AddLogger(Output != null ? new OutputLogger(Output) : ConsoleLogger.Default);
            if (!config.GetLoggers().OfType<ConsoleLogger>().Any())
                config = config.AddLogger(ConsoleLogger.Default);

            if (!config.GetColumnProviders().Any())
                config = config.AddColumnProvider(DefaultColumnProviders.Instance);

            return config!;
        }

        private static void ValidateSummary(Summary summary, WarningCollectingLogger warnings, bool allowWarnings)
        {
            Assert.False(summary.HasCriticalValidationErrors, "The \"Summary\" should have NOT \"HasCriticalValidationErrors\"");

            Assert.True(summary.Reports.Any(), "The \"Summary\" should contain at least one \"BenchmarkReport\" in the \"Reports\" collection");

            summary.CheckPlatformLinkerIssues();

            Assert.True(summary.Reports.All(r => r.BuildResult.IsBuildSuccess),
                "The following benchmarks have failed to build: " +
                string.Join(", ", summary.Reports.Where(r => !r.BuildResult.IsBuildSuccess).Select(r => r.BenchmarkCase.DisplayInfo)));

            Assert.True(summary.Reports.All(r => r.ExecuteResults != null),
                "The following benchmarks don't have any execution results: " +
                string.Join(", ", summary.Reports.Where(r => r.ExecuteResults == null).Select(r => r.BenchmarkCase.DisplayInfo)));

            Assert.True(summary.Reports.All(r => r.ExecuteResults.All(er => er.IsSuccess)),
                "All reports should have succeeded to execute");

            Assert.True(allowWarnings || warnings.Warnings.Count == 0,
                "The run should not log any warnings:" + Environment.NewLine + string.Join(Environment.NewLine, warnings.Warnings));
        }

        protected IConfig CreateSimpleConfig(OutputLogger? logger = null, Job? job = null)
        {
            var baseConfig = job == null ? (IConfig) new SingleRunFastConfig() : new SingleJobConfig(job);
            return baseConfig
                .AddLogger(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
        }

        protected static IReadOnlyList<string> GetSingleStandardOutput(Summary summary)
            => summary.Reports.Single().ExecuteResults.Single().StandardOutput;

        protected static IReadOnlyList<string> GetCombinedStandardOutput(Summary summary)
            => summary.Reports.SelectMany(r => r.ExecuteResults).SelectMany(e => e.StandardOutput).ToArray();
    }
}
