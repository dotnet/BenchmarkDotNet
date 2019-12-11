using System;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Tests
{
    public class ConfigParserTests
    {
        public ITestOutputHelper Output { get; }

        public ConfigParserTests(ITestOutputHelper output) => Output = output;

        [Theory]
        [InlineData("--job=dry", "--exporters", "html", "rplot")]
        [InlineData("--JOB=dry", "--EXPORTERS", "html", "rplot")] // case insensitive
        [InlineData("-j", "dry", "-e", "html", "rplot")] // alias
        public void SimpleConfigParsedCorrectly(params string[] args)
        {
            var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(3, config.GetExporters().Count()); // rplot must come together with CsvMeasurementsExporter
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());
            Assert.Contains(CsvMeasurementsExporter.Default, config.GetExporters());

            Assert.Empty(config.GetColumnProviders());
            Assert.Empty(config.GetDiagnosers());
            Assert.Empty(config.GetAnalysers());
            Assert.Empty(config.GetLoggers());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParsedCorrectly()
        {
            var config = ConfigParser.Parse(new[] { "--job=Dry" }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());
        }

        [Fact]
        public void UserCanSpecifyHowManyTimesTheBenchmarkShouldBeExecuted()
        {
            const int launchCount = 4;
            const int warmupCount = 1;
            const int iterationTime = 250;
            const int iterationCount = 20;

            var config = ConfigParser.Parse(new[]
            {
                "--LaunchCount", launchCount.ToString(),
                "--warmupCount",  warmupCount.ToString(),
                "--iterationTime", iterationTime.ToString(),
                "--iterationCount", iterationCount.ToString()
            }, new OutputLogger(Output)).config;

            var job = config.GetJobs().Single();

            Assert.Equal(launchCount, job.Run.LaunchCount);
            Assert.Equal(warmupCount, job.Run.WarmupCount);
            Assert.Equal(TimeInterval.FromMilliseconds(iterationTime), job.Run.IterationTime);
            Assert.Equal(iterationCount, job.Run.IterationCount);
        }

        [Fact]
        public void UserCanEasilyRequestToRunTheBenchmarkOncePerIteration()
        {
            var configEasy = ConfigParser.Parse(new[] { "--runOncePerIteration" }, new OutputLogger(Output)).config;

            var easyJob = configEasy.GetJobs().Single();

            Assert.Equal(1, easyJob.Run.UnrollFactor);
            Assert.Equal(1, easyJob.Run.InvocationCount);
        }

        [Fact]
        public void UserCanChooseStrategy()
        {
            var configEasy = ConfigParser.Parse(new[] { "--strategy", "ColdStart" }, new OutputLogger(Output)).config;

            var job = configEasy.GetJobs().Single();

            Assert.Equal(RunStrategy.ColdStart, job.Run.RunStrategy);
        }

        [FactDotNetCoreOnly("When CommandLineParser wants to display help, it tries to get the Title of the Entry Assembly which is an xunit runner, which has no Title and fails..")]
        public void UnknownConfigMeansFailure()
        {
            Assert.False(ConfigParser.Parse(new[] { "--unknown" }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void EmptyArgsMeansConfigWithoutJobs()
        {
            var config = ConfigParser.Parse(Array.Empty<string>(), new OutputLogger(Output)).config;

            Assert.Empty(config.GetJobs());
        }

        [Fact]
        public void NonExistingPathMeansFailure()
        {
            string nonExistingFile = Path.Combine(Path.GetTempPath(), "veryUniqueFileName.exe");

            Assert.False(ConfigParser.Parse(new[] { "--cli", nonExistingFile }, new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(new[] { "--coreRun", nonExistingFile }, new OutputLogger(Output)).isSuccess);
        }

        [FactDotNetCoreOnly("Detecting current version of .NET Core works only for .NET Core processes")]
        public void CoreRunConfigParsedCorrectlyWhenRuntimeNotSpecified()
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var fakeRestorePackages = Path.GetTempPath();
            var config = ConfigParser.Parse(new[] { "--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath, "--packages", fakeRestorePackages }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CoreRunToolchain toolchain = config.GetJobs().Single().GetToolchain() as CoreRunToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(RuntimeInformation.GetCurrentRuntime().MsBuildMoniker, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker); // runtime was not specified so the current was used
            Assert.Equal(fakeCoreRunPath, toolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath.FullName);
            Assert.Equal(fakeRestorePackages, toolchain.RestorePath.FullName);
        }

        [Fact]
        public void CoreRunConfigParsedCorrectlyWhenRuntimeSpecified()
        {
            const string runtime = "netcoreapp3.0";
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var fakeRestorePackages = Path.GetTempPath();
            var config = ConfigParser.Parse(new[] { "--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath, "--packages", fakeRestorePackages, "-r", runtime }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CoreRunToolchain toolchain = config.GetJobs().Single().GetToolchain() as CoreRunToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(runtime, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker); // runtime was provided and used
            Assert.Equal(fakeCoreRunPath, toolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath.FullName);
            Assert.Equal(fakeRestorePackages, toolchain.RestorePath.FullName);
        }

        [Fact]
        public void UserCanSpecifyMultipleCoreRunPaths()
        {
            var fakeCoreRunPath_1 = typeof(object).Assembly.Location;
            var fakeCoreRunPath_2 = typeof(ConfigParserTests).Assembly.Location;

            var config = ConfigParser.Parse(new[] { "--job=Dry", "--coreRun", fakeCoreRunPath_1, fakeCoreRunPath_2 }, new OutputLogger(Output)).config;

            var jobs = config.GetJobs().ToArray();
            Assert.Equal(2, jobs.Length);
            Assert.Single(jobs.Where(job => job.GetToolchain() is CoreRunToolchain toolchain && toolchain.SourceCoreRun.FullName == fakeCoreRunPath_1));
            Assert.Single(jobs.Where(job => job.GetToolchain() is CoreRunToolchain toolchain && toolchain.SourceCoreRun.FullName == fakeCoreRunPath_2));
            Assert.Equal(2, jobs.Select(job => job.Id).Distinct().Count()); // each job must have a unique ID
        }

        [Fact]
        public void MonoPathParsedCorrectly()
        {
            var fakeMonoPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "-r", "mono", "--monoPath", fakeMonoPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is MonoRuntime mono && mono.CustomPath == fakeMonoPath));
        }

        [FactWindowsOnly("Testing local builds of Full .NET Framework is supported only on Windows")]
        public void ClrVersionParsedCorrectly()
        {
            const string clrVersion = "secret";
            var config = ConfigParser.Parse(new[] { "--clrVersion", clrVersion }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime clr && clr.Version == clrVersion));
        }

        [Fact]
        public void CoreRtPathParsedCorrectly()
        {
            var fakeCoreRtPath =  new FileInfo(typeof(ConfigParserTests).Assembly.Location).Directory;
            var config = ConfigParser.Parse(new[] { "-r", "corert30", "--ilcPath", fakeCoreRtPath.FullName }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CoreRtToolchain toolchain = config.GetJobs().Single().GetToolchain() as CoreRtToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakeCoreRtPath.FullName, toolchain.IlcPath);
        }

        [Theory]
        [InlineData("netcoreapp2.0")]
        [InlineData("netcoreapp2.1")]
        [InlineData("netcoreapp2.2")]
        [InlineData("netcoreapp3.0")]
        public void DotNetCliParsedCorrectly(string tfm)
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "-r", tfm, "--cli", fakeDotnetCliPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath);
        }

        [Theory]
        [InlineData(ConfigOptions.JoinSummary, "--join")]
        [InlineData(ConfigOptions.KeepBenchmarkFiles, "--keepFiles")]
        [InlineData(ConfigOptions.DontOverwriteResults, "--noOverwrite")]
        [InlineData(ConfigOptions.StopOnFirstError, "--stopOnFirstError")]
        [InlineData(ConfigOptions.DisableLogFile, "--disableLogFile" )]
        [InlineData(
            ConfigOptions.JoinSummary |
            ConfigOptions.KeepBenchmarkFiles |
            ConfigOptions.DontOverwriteResults |
            ConfigOptions.StopOnFirstError |
            ConfigOptions.DisableLogFile, "--join", "--keepFiles", "--noOverwrite", "--stopOnFirstError", "--disableLogFile")]
        [InlineData(
            ConfigOptions.JoinSummary |
            ConfigOptions.KeepBenchmarkFiles |
            ConfigOptions.StopOnFirstError, "--join", "--keepFiles", "--stopOnFirstError")]
        public void ConfigOptionsParsedCorrectly(ConfigOptions expectedConfigOption, params string[] configOptionArgs)
        {
            var config = ConfigParser.Parse(configOptionArgs, new OutputLogger(Output)).config;
            Assert.Equal(expectedConfigOption, config.Options);
            Assert.NotEqual(ConfigOptions.Default, config.Options);
        }

        [Fact]
        public void PackagesPathParsedCorrectly()
        {
            var fakeRestoreDirectory = new FileInfo(typeof(object).Assembly.Location).Directory.FullName;
            var config = ConfigParser.Parse(new[] { "-r", "netcoreapp3.0", "--packages", fakeRestoreDirectory }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakeRestoreDirectory, ((DotNetCliGenerator)toolchain.Generator).PackagesPath);
        }

        [Fact]
        public void UserCanSpecifyBuildTimeout()
        {
            const int timeoutInSeconds = 10;
            var config = ConfigParser.Parse(new[] { "-r", "netcoreapp3.0", "--buildTimeout", timeoutInSeconds.ToString() }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(timeoutInSeconds, ((DotNetCliBuilder)toolchain.Builder).Timeout.TotalSeconds);
        }

        [Fact]
        public void WhenUserDoesNotSpecifyTimeoutTheDefaultValueIsUsed()
        {
            var config = ConfigParser.Parse(new[] { "-r", "netcoreapp3.0" }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(NetCoreAppSettings.DefaultBuildTimeout, ((DotNetCliBuilder)toolchain.Builder).Timeout);
        }

        [Theory]
        [InlineData("net461")]
        [InlineData("net462")]
        [InlineData("net47")]
        [InlineData("net471")]
        [InlineData("net472")]
        public void NetFrameworkMonikerParsedCorrectly(string tfm)
        {
            var config = ConfigParser.Parse(new[] { "-r", tfm }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjClassicNetToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjClassicNetToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Fact]
        public void CanCompareFewDifferentRuntimes()
        {
            var config = ConfigParser.Parse(new[] { "--runtimes", "net461", "MONO", "netcoreapp3.0", "CoreRT30"}, new OutputLogger(Output)).config;

            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime clrRuntime && clrRuntime.MsBuildMoniker == "net461"));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is MonoRuntime));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is CoreRuntime coreRuntime && coreRuntime.MsBuildMoniker == "netcoreapp3.0" && coreRuntime.RuntimeMoniker == RuntimeMoniker.NetCoreApp30));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is CoreRtRuntime coreRtRuntime && coreRtRuntime.MsBuildMoniker == "netcoreapp3.0" && coreRtRuntime.RuntimeMoniker == RuntimeMoniker.CoreRt30));
        }

        [Theory]
        [InlineData(ThresholdUnit.Ratio, 5)]
        [InlineData(ThresholdUnit.Milliseconds, 10)]
        public void CanUseStatisticalTestsToCompareFewDifferentRuntimes(ThresholdUnit thresholdUnit, double thresholdValue)
        {
            var config = ConfigParser.Parse(new[]
            {
                "--runtimes", "netcoreapp2.1", "netcoreapp2.2",
                "--statisticalTest", $"{thresholdValue.ToString(CultureInfo.InvariantCulture)}{thresholdUnit.ToShortName()}"
            }, new OutputLogger(Output)).config;

            var mockSummary = MockFactory.CreateSummary(config);

            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.False(config.GetJobs().Last().Meta.Baseline);

            var statisticalTestColumn = config.GetColumnProviders().SelectMany(columnProvider => columnProvider.GetColumns(mockSummary)).OfType<StatisticalTestColumn>().Single();

            Assert.Equal(StatisticalTestKind.MannWhitney, statisticalTestColumn.Kind);
            Assert.Equal(Threshold.Create(thresholdUnit, thresholdUnit == ThresholdUnit.Ratio ? thresholdValue / 100.0 : thresholdValue), statisticalTestColumn.Threshold);
        }

        [Fact]
        public void SpecifyingInvalidStatisticalTestsThresholdMeansFailure()
        {
            Assert.False(ConfigParser.Parse(new[] {"--statisticalTest", "not a number" }, new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(new[] {"--statisticalTest", "1unknownUnit" }, new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(new[] {"--statisticalTest", "1 unknownUnit" }, new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(new[] {"--statisticalTest", "%1" }, new OutputLogger(Output)).isSuccess); // reverse order - a typo
        }

        [Fact]
        public void CanParseHardwareCounters()
        {
            var config = ConfigParser.Parse(new[] { "--counters", $"{nameof(HardwareCounter.CacheMisses)}+{nameof(HardwareCounter.InstructionRetired)}"}, new OutputLogger(Output)).config;

            Assert.Equal(2, config.GetHardwareCounters().Count());
            Assert.Single(config.GetHardwareCounters().Where(counter => counter == HardwareCounter.CacheMisses));
            Assert.Single(config.GetHardwareCounters().Where(counter => counter == HardwareCounter.InstructionRetired));
        }

        [Fact]
        public void InvalidHardwareCounterNameMeansFailure()
        {
            Assert.False(ConfigParser.Parse(new[] { "--counters", "WRONG_NAME" }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void TooManyHardwareCounterNameMeansFailure()
        {
            Assert.False(ConfigParser.Parse(new[] { "--counters", "Timer+TotalIssues+BranchInstructions+CacheMisses" }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void CanParseDisassemblerWithCustomRecursiveDepth()
        {
            const int depth = 123;

            var config = ConfigParser.Parse(new[] { "--disasm", "--disasmDepth", depth.ToString()}, new OutputLogger(Output)).config;

            var diagnoser = config.GetDiagnosers().OfType<DisassemblyDiagnoser>().Single();

            Assert.Equal(depth, diagnoser.Config.RecursiveDepth);
            Assert.True(diagnoser.Config.PrintPrologAndEpilog); // we want this option to be enabled by default for command line users
        }

        [Fact]
        public void CanParseInfo()
        {
            var config = ConfigParser.Parse(new[] { "--info" }, new OutputLogger(Output)).options;

            Assert.True(config.PrintInformation);
        }

        [Fact]
        public void UserCanSpecifyCustomDefaultJobAndOverwriteItsSettingsViaConsoleArgs()
        {
            var globalConfig = DefaultConfig.Instance
                .With(Job.Default
                    .WithWarmupCount(1)
                    .AsDefault());

            var parsedConfig = ConfigParser.Parse(new[] { "--warmupCount", "2"}, new OutputLogger(Output), globalConfig).config;

            Assert.Equal(2, parsedConfig.GetJobs().Single().Run.WarmupCount);
            Assert.False(parsedConfig.GetJobs().Single().Meta.IsDefault); // after the merge the job is not "default" anymore
        }

        [Fact]
        public void UserCanSpecifyCustomMaxParameterColumnWidth()
        {
            const int customValue = 1234;

            var globalConfig = DefaultConfig.Instance;

            Assert.NotEqual(customValue, globalConfig.SummaryStyle.MaxParameterColumnWidth);

            var parsedConfig = ConfigParser.Parse(new[] { "--maxWidth", customValue.ToString() }, new OutputLogger(Output), globalConfig).config;

            Assert.Equal(customValue, parsedConfig.SummaryStyle.MaxParameterColumnWidth);
        }

        [Fact]
        public void UserCanSpecifyEnvironmentVariables()
        {
            const string key = "A_VERY_NICE_ENV_VAR";
            const string value = "enabled";

            var parsedConfig = ConfigParser.Parse(new[] { "--envVars", $"{key}:{value}" }, new OutputLogger(Output)).config;

            var job = parsedConfig.GetJobs().Single();
            var envVar = job.Environment.EnvironmentVariables.Single();

            Assert.Equal(key, envVar.Key);
            Assert.Equal(value, envVar.Value);
        }

        [Fact]
        public void InvalidEnvVarAreRecognized()
        {
            Assert.False(ConfigParser.Parse(new[] { "--envVars", "INVALID_NO_SEPARATOR" }, new OutputLogger(Output)).isSuccess);
        }
    }
}