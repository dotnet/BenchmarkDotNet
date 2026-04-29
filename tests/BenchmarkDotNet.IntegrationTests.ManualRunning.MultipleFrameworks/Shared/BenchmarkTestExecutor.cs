using AwesomeAssertions;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using TUnit.Core.Interfaces;

namespace BenchmarkDotNet.Tests;

public class BenchmarkTestExecutor
{
    protected ITestOutput Output => TestContext.Current!.Output!;

    /// <summary>
    /// Runs Benchmarks with the most simple config (SingleRunFastConfig)
    /// combined with any benchmark config applied to TBenchmark (via an attribute)
    /// By default will verify if every benchmark was successfully executed
    /// </summary>
    /// <typeparam name="TBenchmark">type that defines Benchmarks</typeparam>
    /// <param name="config">Optional custom config to be used instead of the default</param>
    /// <param name="fullValidation">Optional: disable validation (default = true/enabled)</param>
    /// <returns>The summary from the benchmark run</returns>
    public Reports.Summary CanExecute<TBenchmark>(IConfig? config = null, bool fullValidation = true)
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
    public Reports.Summary CanExecute(Type type, IConfig? config = null, bool fullValidation = true)
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

        // Make sure we ALWAYS combine the Config (default or passed in) with any Config applied to the Type/Class
        var summary = BenchmarkRunner.Run(type, config);

        if (fullValidation)
        {
            summary.HasCriticalValidationErrors.Should().BeFalse("The \"Summary\" should have NOT \"HasCriticalValidationErrors\"");
            summary.Reports.Any().Should().BeTrue("The \"Summary\" should contain at least one \"BenchmarkReport\" in the \"Reports\" collection");

            summary.CheckPlatformLinkerIssues();

            summary.Reports.Should().OnlyContain(r => r.BuildResult.IsBuildSuccess,
                "The following benchmarks have failed to build: " +
                string.Join(", ", summary.Reports.Where(r => !r.BuildResult.IsBuildSuccess).Select(r => r.BenchmarkCase.DisplayInfo)));

            summary.Reports.Should().OnlyContain(r => r.ExecuteResults != null,
                "The following benchmarks have failed to execute: " +
                string.Join(", ", summary.Reports.Where(r => r.ExecuteResults == null || r.ExecuteResults.Any(er => !er.IsSuccess)).Select(r => r.BenchmarkCase.DisplayInfo))); ;

            summary.Reports.SelectMany(x => x.ExecuteResults).Should().OnlyContain(r => r.IsSuccess,
                "All reports should have succeeded to execute");
        }

        return summary;
    }

    protected IConfig CreateSimpleConfig(OutputLogger? logger = null, Job? job = null)
    {
        var baseConfig = job == null ? (IConfig)new SingleRunFastConfig() : new SingleJobConfig(job);
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