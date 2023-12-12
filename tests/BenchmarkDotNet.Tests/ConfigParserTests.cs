using System;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

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

        [FactEnvSpecific(
            "When CommandLineParser wants to display help, it tries to get the Title of the Entry Assembly which is an xunit runner, which has no Title and fails..",
            EnvRequirement.DotNetCoreOnly)]
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

        [FactEnvSpecific("Detecting current version of .NET Core works only for .NET Core processes", EnvRequirement.DotNetCoreOnly)]
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

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.FullFrameworkOnly)]
        public void SpecifyingCoreRunWithFullFrameworkTargetsMostRecentTfm()
        {
            var fakePath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "--corerun", fakePath }, new OutputLogger(Output)).config;

            Job coreRunJob = config.GetJobs().Single();

            CoreRunToolchain coreRunToolchain = (CoreRunToolchain)coreRunJob.GetToolchain();
            DotNetCliGenerator generator = (DotNetCliGenerator)coreRunToolchain.Generator;
            Assert.Equal("net9.0", generator.TargetFrameworkMoniker);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
        public void SpecifyingCoreRunAndRuntimeCreatesTwoJobs()
        {
            const string runtime = "net8.0";
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var fakeRestorePackages = Path.GetTempPath();
            var config = ConfigParser.Parse(new[] { "--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath, "--packages", fakeRestorePackages, "-r", runtime }, new OutputLogger(Output)).config;

            Assert.Equal(2, config.GetJobs().Count());

            Job coreRunJob = config.GetJobs().Single(job => job.GetToolchain() is CoreRunToolchain);
            Job runtimeJob = config.GetJobs().Single(job => job.GetToolchain() is CsProjCoreToolchain);

            CoreRunToolchain coreRunToolchain = (CoreRunToolchain)coreRunJob.GetToolchain();
            DotNetCliGenerator generator = (DotNetCliGenerator)coreRunToolchain.Generator;
            Assert.Equal(RuntimeInformation.GetCurrentRuntime().MsBuildMoniker, generator.TargetFrameworkMoniker);
            Assert.Equal(fakeCoreRunPath, coreRunToolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, coreRunToolchain.CustomDotNetCliPath.FullName);
            Assert.Equal(fakeRestorePackages, coreRunToolchain.RestorePath.FullName);

            CsProjCoreToolchain coreToolchain = (CsProjCoreToolchain)runtimeJob.GetToolchain();
            generator = (DotNetCliGenerator)coreToolchain.Generator;
            Assert.Equal(runtime, ((DotNetCliGenerator)coreToolchain.Generator).TargetFrameworkMoniker);
            Assert.Equal(fakeDotnetCliPath, coreToolchain.CustomDotNetCliPath);
            Assert.Equal(fakeRestorePackages, generator.PackagesPath);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
        public void FirstJobIsBaseline_RuntimesCoreRun()
        {
            const string runtime1 = "net5.0";
            const string runtime2 = "net6.0";
            string fakePath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "--runtimes", runtime1, runtime2, "--coreRun", fakePath }, new OutputLogger(Output)).config;

            Assert.Equal(3, config.GetJobs().Count());
            Job baselineJob = config.GetJobs().Single(job => job.Meta.Baseline == true);
            Assert.False(baselineJob.GetToolchain() is CoreRunToolchain);
            Assert.Equal(runtime1, ((DotNetCliGenerator)baselineJob.GetToolchain().Generator).TargetFrameworkMoniker);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
        public void FirstJobIsBaseline_CoreRunsRuntimes()
        {
            const string runtime1 = "net5.0";
            const string runtime2 = "net6.0";
            string fakePath1 = typeof(object).Assembly.Location;
            string fakePath2 = typeof(FactAttribute).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "--coreRun", fakePath1, fakePath2, "--runtimes", runtime1, runtime2 }, new OutputLogger(Output)).config;

            Assert.Equal(4, config.GetJobs().Count());
            Job baselineJob = config.GetJobs().Single(job => job.Meta.Baseline == true);
            Assert.Equal(fakePath1, ((CoreRunToolchain)baselineJob.GetToolchain()).SourceCoreRun.FullName);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
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

        [FactEnvSpecific("Testing local builds of Full .NET Framework is supported only on Windows", EnvRequirement.WindowsOnly)]
        public void ClrVersionParsedCorrectly()
        {
            const string clrVersion = "secret";
            var config = ConfigParser.Parse(new[] { "--clrVersion", clrVersion }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime clr && clr.Version == clrVersion));
        }

        [Fact]
        public void IlCompilerPathParsedCorrectly()
        {
            var fakePath =  new FileInfo(typeof(ConfigParserTests).Assembly.Location).Directory;
            var config = ConfigParser.Parse(new[] { "-r", "nativeaot60", "--ilcPackages", fakePath.FullName }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            NativeAotToolchain toolchain = config.GetJobs().Single().GetToolchain() as NativeAotToolchain;
            Assert.NotNull(toolchain);
            Generator generator = (Generator)toolchain.Generator;
            Assert.Equal(fakePath.FullName, generator.Feeds["local"]);
        }

        [Theory]
        [InlineData("netcoreapp2.0", true)]
        [InlineData("netcoreapp2.1", true)]
        [InlineData("netcoreapp2.2", true)]
        [InlineData("netcoreapp3.0", true)]
        [InlineData("net462", false)]
        [InlineData("net48", false)]
        public void DotNetCliParsedCorrectly(string tfm, bool isCore)
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "-r", tfm, "--cli", fakeDotnetCliPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain();
            if (isCore)
            {
                Assert.True(toolchain is CsProjCoreToolchain);
                Assert.Equal(fakeDotnetCliPath, ((CsProjCoreToolchain) toolchain).CustomDotNetCliPath);
            }
            else
            {
                Assert.True(toolchain is CsProjClassicNetToolchain);
                Assert.Equal(fakeDotnetCliPath, ((CsProjClassicNetToolchain) toolchain).CustomDotNetCliPath);
            }
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData(ConfigOptions.JoinSummary, "--join")]
        [InlineData(ConfigOptions.KeepBenchmarkFiles, "--keepFiles")]
        [InlineData(ConfigOptions.DontOverwriteResults, "--noOverwrite")]
        [InlineData(ConfigOptions.StopOnFirstError, "--stopOnFirstError")]
        [InlineData(ConfigOptions.DisableLogFile, "--disableLogFile" )]
        [InlineData(ConfigOptions.LogBuildOutput, "--logBuildOutput")]
        [InlineData(ConfigOptions.GenerateMSBuildBinLog | ConfigOptions.KeepBenchmarkFiles, "--generateBinLog")]
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
        public void WhenConfigOptionsFlagsAreNotSpecifiedTheyAreNotSet()
        {
            var config = ConfigParser.Parse(Array.Empty<string>(), new OutputLogger(Output)).config;
            Assert.Equal(ConfigOptions.Default, config.Options);
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
            Assert.Equal(timeoutInSeconds, config.BuildTimeout.TotalSeconds);
        }

        [Fact]
        public void WhenUserDoesNotSpecifyTimeoutTheDefaultValueIsUsed()
        {
            var config = ConfigParser.Parse(new[] { "-r", "netcoreapp3.0" }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(DefaultConfig.Instance.BuildTimeout, config.BuildTimeout);
        }

        [Theory]
        [InlineData("net461")]
        [InlineData("net462")]
        [InlineData("net47")]
        [InlineData("net471")]
        [InlineData("net472")]
        [InlineData("net48")]
        [InlineData("net481")]
        public void NetFrameworkMonikerParsedCorrectly(string tfm)
        {
            var config = ConfigParser.Parse(new[] { "-r", tfm }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjClassicNetToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjClassicNetToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData("net50")]
        [InlineData("net60")]
        [InlineData("net70")]
        [InlineData("net80")]
        [InlineData("net90")]
        public void NetMonikersAreRecognizedAsNetCoreMonikers(string tfm)
        {
            var config = ConfigParser.Parse(new[] { "-r", tfm }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData("net5.0-windows")]
        [InlineData("net5.0-ios")]
        public void PlatformSpecificMonikersAreSupported(string msBuildMoniker)
        {
            var config = ConfigParser.Parse(new[] { "-r", msBuildMoniker }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(msBuildMoniker, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Fact]
        public void CanCompareFewDifferentRuntimes()
        {
            var config = ConfigParser.Parse(new[] { "--runtimes", "net462", "MONO", "netcoreapp3.0", "nativeaot6.0", "nativeAOT7.0", "nativeAOT8.0" }, new OutputLogger(Output)).config;

            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime clrRuntime && clrRuntime.MsBuildMoniker == "net462"));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is MonoRuntime));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is CoreRuntime coreRuntime && coreRuntime.MsBuildMoniker == "netcoreapp3.0" && coreRuntime.RuntimeMoniker == RuntimeMoniker.NetCoreApp30));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net6.0" && nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot60));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net7.0" && nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot70));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net8.0" && nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot80));
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

            Assert.Equal(depth, diagnoser.Config.MaxDepth);
        }

        [Fact]
        public void WhenCustomDisassemblerSettingsAreProvidedItsEnabledByDefault()
        {
            Verify(new[] { "--disasmDepth", "2" });
            Verify(new[] { "--disasmFilter", "*" });

            void Verify(string[] args)
            {
                var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;
                Assert.Single(config.GetDiagnosers().OfType<DisassemblyDiagnoser>());
            }
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
                .AddJob(Job.Default
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

        [Theory]
        [InlineData(Platform.AnyCpu)]
        [InlineData(Platform.X86)]
        [InlineData(Platform.X64)]
        [InlineData(Platform.Arm)]
        [InlineData(Platform.Arm64)]
        [InlineData(Platform.LoongArch64)]
        public void UserCanSpecifyProcessPlatform(Platform platform)
        {
            var parsedConfig = ConfigParser.Parse(new[] { "--platform", platform.ToString() }, new OutputLogger(Output)).config;

            var job = parsedConfig.GetJobs().Single();
            var parsed = job.Environment.Platform;

            Assert.Equal(platform, parsed);
        }

        [Fact]
        public void InvalidEnvVarAreRecognized()
        {
            Assert.False(ConfigParser.Parse(new[] { "--envVars", "INVALID_NO_SEPARATOR" }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void UserCanSpecifyNoForceGCs()
        {
            var parsedConfiguration = ConfigParser.Parse(new[] { "--noForcedGCs" }, new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);

            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                Assert.False(job.Environment.Gc.Force);
            }
        }

        [Fact]
        public void UsersCanSpecifyWithoutOverheadEvalution()
        {
            var parsedConfiguration = ConfigParser.Parse(new[] { "--noOverheadEvaluation" }, new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);

            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                Assert.False(job.Accuracy.EvaluateOverhead);
            }
        }

        [Fact(Skip = "This should be handled somehow at CommandLineParser level. See https://github.com/commandlineparser/commandline/pull/892")]
        public void UserCanSpecifyWasmArgs()
        {
            var parsedConfiguration = ConfigParser.Parse(new[] { "--runtimes", "wasm", "--wasmArgs", "--expose_wasm --module" }, new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmRuntime = Assert.IsType<WasmRuntime>(job.Environment.Runtime);
                Assert.Equal(" --expose_wasm --module", wasmRuntime.JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmArgsUsingEquals()
        {
            var parsedConfiguration = ConfigParser.Parse(new[] { "--runtimes", "wasm", "--wasmArgs=--expose_wasm --module" }, new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmRuntime = Assert.IsType<WasmRuntime>(job.Environment.Runtime);
                Assert.Equal("--expose_wasm --module", wasmRuntime.JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmArgsViaResponseFile()
        {
            var tempResponseFile = Path.GetRandomFileName();
            File.WriteAllLines(tempResponseFile, new[]
            {
                "--runtimes wasm",
                "--wasmArgs \"--expose_wasm --module\""
            });
            var parsedConfiguration = ConfigParser.Parse(new[] { $"@{tempResponseFile}" }, new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmRuntime = Assert.IsType<WasmRuntime>(job.Environment.Runtime);
                // We may need change assertion to just "--expose_wasm --module"
                // if https://github.com/commandlineparser/commandline/pull/892 lands
                Assert.Equal(" --expose_wasm --module", wasmRuntime.JavaScriptEngineArguments);
            }
        }

        [Theory]
        [InlineData("--filter abc", "--filter *")]
        [InlineData("-f abc", "--filter *")]
        [InlineData("-f *", "--filter *")]
        [InlineData("--runtimes net7.0 --join", "--filter * --join --runtimes net7.0")]
        [InlineData("--join abc", "--filter * --join")]
        public void CheckUpdateValidArgs(string strArgs, string expected)
        {
            var args = strArgs.Split();
            _ = ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => options.Filters = new[] { "*" });

            Assert.Equal(expected.Split(), updatedArgs);
        }

        [Theory]
        [InlineData("--filter abc -f abc")]
        [InlineData("--runtimes net")]
        public void CheckUpdateInvalidArgs(string strArgs)
        {
            var args = strArgs.Split();
            bool isSuccess = ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => options.Filters = new[] { "*" });

            Assert.Null(updatedArgs);
            Assert.False(isSuccess);
        }
    }
}
