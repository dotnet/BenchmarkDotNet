using AwesomeAssertions;
using AwesomeAssertions.Execution;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.OpenMetrics;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.NativeAot;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using System.Reflection;

namespace BenchmarkDotNet.Tests
{
    public class ConfigParserTests
    {
        public ITestOutputHelper Output { get; }
        public static TheoryData<string, IExporter[]> Exporters => new()
        {
            { "csv", [CsvExporter.Default] },
            { "csvmeasurements", [CsvMeasurementsExporter.Default] },
            { "html", [HtmlExporter.Default] },
            { "markdown", [MarkdownExporter.Default] },
            { "atlassian", [MarkdownExporter.Atlassian] },
            { "stackoverflow", [MarkdownExporter.StackOverflow] },
            { "github", [MarkdownExporter.GitHub] },
            { "plain", [PlainExporter.Default] },
            { "rplot", [CsvMeasurementsExporter.Default, RPlotExporter.Default] },
            { "json", [JsonExporter.Default] },
            { "briefjson", [JsonExporter.Brief] },
            { "fulljson", [JsonExporter.Full] },
            { "asciidoc", [AsciiDocExporter.Default] },
            { "xml", [XmlExporter.Default] },
            { "briefxml", [XmlExporter.Brief] },
            { "fullxml", [XmlExporter.Full] },
            { "openmetrics", [OpenMetricsExporter.Default] }
        };

        public ConfigParserTests(ITestOutputHelper output) => Output = output;

        [Theory]
        [InlineData("--job=dry", "--exporters", "html", "rplot")]
        [InlineData("--JOB=dry", "--EXPORTERS", "html", "rplot")] // case insensitive
        [InlineData("-j", "dry", "-e", "html", "rplot")] // alias
        public void SimpleConfigParsedCorrectly(params string[] args)
        {
            var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;

            Assert.NotNull(config);
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

        [Theory]
        [MemberData(nameof(Exporters), DisableDiscoveryEnumeration = true)]
        public void ExportersAreParsedCorrectly(string exporter, IExporter[] expectedExporters)
        {
            var config = ConfigParser.Parse(["--exporters", exporter], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(expectedExporters, config.GetExporters().ToArray());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParsedCorrectly()
        {
            var config = ConfigParser.Parse(["--job=Dry"], new OutputLogger(Output)).config;

            Assert.NotNull(config);
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

            var config = ConfigParser.Parse(
            [
                "--LaunchCount", launchCount.ToString(),
                "--warmupCount", warmupCount.ToString(),
                "--iterationTime", iterationTime.ToString(),
                "--iterationCount", iterationCount.ToString()
            ], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            var job = config.GetJobs().Single();

            Assert.Equal(launchCount, job.Run.LaunchCount);
            Assert.Equal(warmupCount, job.Run.WarmupCount);
            Assert.Equal(TimeInterval.FromMilliseconds(iterationTime), job.Run.IterationTime);
            Assert.Equal(iterationCount, job.Run.IterationCount);
        }

        [Fact]
        public void UserCanEasilyRequestToRunTheBenchmarkOncePerIteration()
        {
            var configEasy = ConfigParser.Parse(["--runOncePerIteration"], new OutputLogger(Output)).config;

            Assert.NotNull(configEasy);
            var easyJob = configEasy.GetJobs().Single();

            Assert.Equal(1, easyJob.Run.UnrollFactor);
            Assert.Equal(1, easyJob.Run.InvocationCount);
        }

        [Fact]
        public void UserCanChooseStrategy()
        {
            var configEasy = ConfigParser.Parse(["--strategy", "ColdStart"], new OutputLogger(Output)).config;

            Assert.NotNull(configEasy);
            var job = configEasy.GetJobs().Single();

            Assert.Equal(RunStrategy.ColdStart, job.Run.RunStrategy);
        }

        [Fact]
        public void UserCanChooseInProcessAndStrategyMonitoring()
        {
            var configEasy = ConfigParser.Parse(["--inProcess", "--strategy", "Monitoring"], new OutputLogger(Output)).config;

            Assert.NotNull(configEasy);
            var job = configEasy.GetJobs().Single();

            job.GetToolchain().Should().BeOfType<InProcessEmitToolchain>();
            job.Run.RunStrategy.Should().Be(RunStrategy.Monitoring);
        }

        [FactEnvSpecific(
            "When CommandLineParser wants to display help, it tries to get the Title of the Entry Assembly which is an xunit runner, which has no Title and fails..",
            EnvRequirement.DotNetCoreOnly)]
        public void UnknownConfigMeansFailure()
        {
            Assert.False(ConfigParser.Parse(["--unknown"], new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void EmptyArgsMeansConfigWithoutJobs()
        {
            var config = ConfigParser.Parse([], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Empty(config.GetJobs());
        }

        [Fact]
        public void NonExistingPathMeansFailure()
        {
            string nonExistingFile = Path.Combine(Path.GetTempPath(), "veryUniqueFileName.exe");

            Assert.False(ConfigParser.Parse(["--cli", nonExistingFile], new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(["--coreRun", nonExistingFile], new OutputLogger(Output)).isSuccess);
        }

        [FactEnvSpecific("Detecting current version of .NET Core works only for .NET Core processes", EnvRequirement.DotNetCoreOnly)]
        public void CoreRunConfigParsedCorrectlyWhenRuntimeNotSpecified()
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var fakeRestorePackages = Path.GetTempPath();
            var config = ConfigParser.Parse(["--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath, "--packages", fakeRestorePackages],
                new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            CoreRunToolchain? toolchain = config.GetJobs().Single().GetToolchain() as CoreRunToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(RuntimeInformation.GetCurrentRuntime().MsBuildMoniker,
                ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker); // runtime was not specified so the current was used
            Assert.Equal(fakeCoreRunPath, toolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath?.FullName);
            Assert.Equal(fakeRestorePackages, toolchain.RestorePath?.FullName);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.FullFrameworkOnly)]
        public void SpecifyingCoreRunWithFullFrameworkTargetsMostRecentTfm()
        {
            var fakePath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(["--corerun", fakePath], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Job coreRunJob = config.GetJobs().Single();

            CoreRunToolchain coreRunToolchain = (CoreRunToolchain)coreRunJob.GetToolchain();
            DotNetCliGenerator generator = (DotNetCliGenerator)coreRunToolchain.Generator;
            Assert.Equal("net11.0", generator.TargetFrameworkMoniker);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
        public void SpecifyingCoreRunAndRuntimeCreatesTwoJobs()
        {
            const string runtime = "net8.0";
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var fakeRestorePackages = Path.GetTempPath();
            var config = ConfigParser
                .Parse(["--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath, "--packages", fakeRestorePackages, "-r", runtime],
                    new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(2, config.GetJobs().Count());

            Job coreRunJob = config.GetJobs().Single(job => job.GetToolchain() is CoreRunToolchain);
            Job runtimeJob = config.GetJobs().Single(job => job.GetToolchain() is CsProjCoreToolchain);

            CoreRunToolchain coreRunToolchain = (CoreRunToolchain)coreRunJob.GetToolchain();
            DotNetCliGenerator generator = (DotNetCliGenerator)coreRunToolchain.Generator;
            Assert.Equal(RuntimeInformation.GetCurrentRuntime().MsBuildMoniker, generator.TargetFrameworkMoniker);
            Assert.Equal(fakeCoreRunPath, coreRunToolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, coreRunToolchain.CustomDotNetCliPath?.FullName);
            Assert.Equal(fakeRestorePackages, coreRunToolchain.RestorePath?.FullName);

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
            var config = ConfigParser.Parse(["--runtimes", runtime1, runtime2, "--coreRun", fakePath], new OutputLogger(Output)).config;

            Assert.NotNull(config);
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
            var config = ConfigParser.Parse(["--coreRun", fakePath1, fakePath2, "--runtimes", runtime1, runtime2], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(4, config.GetJobs().Count());
            Job baselineJob = config.GetJobs().Single(job => job.Meta.Baseline == true);
            Assert.Equal(fakePath1, ((CoreRunToolchain)baselineJob.GetToolchain()).SourceCoreRun.FullName);
        }

        [FactEnvSpecific("It's impossible to determine TFM for CoreRunToolchain if host process is not .NET (Core) process", EnvRequirement.DotNetCoreOnly)]
        public void UserCanSpecifyMultipleCoreRunPaths()
        {
            var fakeCoreRunPath_1 = typeof(object).Assembly.Location;
            var fakeCoreRunPath_2 = typeof(ConfigParserTests).Assembly.Location;

            var config = ConfigParser.Parse(["--job=Dry", "--coreRun", fakeCoreRunPath_1, fakeCoreRunPath_2], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            var jobs = config.GetJobs().ToArray();
            Assert.Equal(2, jobs.Length);
            Assert.Single(jobs, job => job.GetToolchain() is CoreRunToolchain toolchain && toolchain.SourceCoreRun.FullName == fakeCoreRunPath_1);
            Assert.Single(jobs, job => job.GetToolchain() is CoreRunToolchain toolchain && toolchain.SourceCoreRun.FullName == fakeCoreRunPath_2);
        }

        [Fact]
        public void MonoPathParsedCorrectly()
        {
            var fakeMonoPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(["-r", "mono", "--monoPath", fakeMonoPath], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs(), job => job.Environment.Runtime is MonoRuntime mono && mono.CustomPath == fakeMonoPath);
        }

        [FactEnvSpecific("Testing local builds of Full .NET Framework is supported only on Windows", EnvRequirement.WindowsOnly)]
        public void ClrVersionParsedCorrectly()
        {
            const string clrVersion = "secret";
            var config = ConfigParser.Parse(["--clrVersion", clrVersion], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs(), job => job.Environment.Runtime is ClrRuntime clr && clr.Version == clrVersion);
        }

        [Fact]
        public void IlCompilerPathParsedCorrectly()
        {
            var fakePath = new FileInfo(typeof(ConfigParserTests).Assembly.Location).Directory!;
            var config = ConfigParser.Parse(["-r", "nativeaot60", "--ilcPackages", fakePath.FullName], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            NativeAotToolchain? toolchain = config.GetJobs().Single().GetToolchain() as NativeAotToolchain;
            Assert.NotNull(toolchain);
            Generator generator = (Generator)toolchain.Generator;
            Assert.Equal(fakePath.FullName, generator.Feeds["local"]);
        }

        [Theory]
        [InlineData("netcoreapp2.0", true)]
        [InlineData("netcoreapp2.1", true)]
        [InlineData("netcoreapp2.2", true)]
        [InlineData("netcoreapp3.0", true)]
        [InlineData("netcoreapp3.1", true)]
        [InlineData("net5.0", true)]
        [InlineData("net6.0", true)]
        [InlineData("net7.0", true)]
        [InlineData("net8.0", true)]
        [InlineData("net9.0", true)]
        [InlineData("net462", false)]
        [InlineData("net472", false)]
        [InlineData("net48", false)]
        public void DotNetCliParsedCorrectly(string tfm, bool isCore)
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(["-r", tfm, "--cli", fakeDotnetCliPath], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain();
            if (isCore)
            {
                Assert.True(toolchain is CsProjCoreToolchain);
                Assert.Equal(fakeDotnetCliPath, ((CsProjCoreToolchain)toolchain).CustomDotNetCliPath);
            }
            else
            {
                Assert.True(toolchain is CsProjClassicNetToolchain);
                Assert.Equal(fakeDotnetCliPath, ((CsProjClassicNetToolchain)toolchain).CustomDotNetCliPath);
            }
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData(ConfigOptions.JoinSummary, "--join")]
        [InlineData(ConfigOptions.KeepBenchmarkFiles, "--keepFiles")]
        [InlineData(ConfigOptions.DontOverwriteResults, "--noOverwrite")]
        [InlineData(ConfigOptions.StopOnFirstError, "--stopOnFirstError")]
        [InlineData(ConfigOptions.DisableLogFile, "--disableLogFile")]
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

            Assert.NotNull(config);
            Assert.Equal(expectedConfigOption, config.Options);
            Assert.NotEqual(ConfigOptions.Default, config.Options);
        }

        [Fact]
        public void WhenConfigOptionsFlagsAreNotSpecifiedTheyAreNotSet()
        {
            var config = ConfigParser.Parse([], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(ConfigOptions.Default, config.Options);
        }

        [Fact]
        public void PackagesPathParsedCorrectly()
        {
            var fakeRestoreDirectory = new FileInfo(typeof(object).Assembly.Location).Directory!.FullName;
            var config = ConfigParser.Parse(["-r", "netcoreapp3.1", "--packages", fakeRestoreDirectory], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakeRestoreDirectory, ((DotNetCliGenerator)toolchain.Generator).PackagesPath);
        }

        [Fact]
        public void UserCanSpecifyBuildTimeout()
        {
            const int timeoutInSeconds = 10;
            var config = ConfigParser.Parse(["-r", "netcoreapp3.1", "--buildTimeout", timeoutInSeconds.ToString()], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(timeoutInSeconds, config.BuildTimeout.TotalSeconds);
        }

        [Fact]
        public void WhenUserDoesNotSpecifyTimeoutTheDefaultValueIsUsed()
        {
            var config = ConfigParser.Parse(["-r", "netcoreapp3.1"], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(DefaultConfig.Instance.BuildTimeout, config.BuildTimeout);
        }

        [Fact]
        public void UserCanSpecifyWakeLock()
        {
            var config = ConfigParser.Parse(["--wakeLock", "Display"], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(WakeLockType.Display, config.WakeLock);
        }

        [Fact]
        public void WhenUserDoesNotSpecifyWakeLockTheDefaultValueIsUsed()
        {
            var config = ConfigParser.Parse([], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(DefaultConfig.Instance.WakeLock, config.WakeLock);
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
            var config = ConfigParser.Parse(["-r", tfm], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            CsProjClassicNetToolchain? toolchain = config.GetJobs().Single().GetToolchain() as CsProjClassicNetToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData("net50")]
        [InlineData("net5.0")]
        [InlineData("net60")]
        [InlineData("net6.0")]
        [InlineData("net70")]
        [InlineData("net7.0")]
        [InlineData("net80")]
        [InlineData("net8.0")]
        [InlineData("net90")]
        [InlineData("net9.0")]
        [InlineData("net10_0")]
        [InlineData("net10.0")]
        [InlineData("net11_0")]
        [InlineData("net11.0")]
        public void NetMonikersAreRecognizedAsNetCoreMonikers(string tfm)
        {
            var config = ConfigParser.Parse(["-r", tfm], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Theory]
        [InlineData("net5.0-windows")]
        [InlineData("net5.0-ios")]
        public void PlatformSpecificMonikersAreSupported(string msBuildMoniker)
        {
            var config = ConfigParser.Parse(["-r", msBuildMoniker], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(msBuildMoniker, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }

        [Fact]
        public void CanCompareFewDifferentRuntimes()
        {
            var config = ConfigParser.Parse(["--runtimes", "net462", "MONO", "netcoreapp2.0", "nativeaot6.0", "nativeAOT7.0", "nativeAOT8.0"],
                new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.Single(config.GetJobs(), job => job.Environment.Runtime is ClrRuntime clrRuntime && clrRuntime.MsBuildMoniker == "net462");
            Assert.Single(config.GetJobs(), job => job.Environment.Runtime is MonoRuntime);
            Assert.Single(config.GetJobs(), job =>
                job.Environment.Runtime is CoreRuntime coreRuntime && coreRuntime.MsBuildMoniker == "netcoreapp2.0" &&
                coreRuntime.RuntimeMoniker == RuntimeMoniker.NetCoreApp20);
            Assert.Single(config.GetJobs(), job =>
                job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net6.0" &&
                nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot60);
            Assert.Single(config.GetJobs(), job =>
                job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net7.0" &&
                nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot70);
            Assert.Single(config.GetJobs(), job =>
                job.Environment.Runtime is NativeAotRuntime nativeAot && nativeAot.MsBuildMoniker == "net8.0" &&
                nativeAot.RuntimeMoniker == RuntimeMoniker.NativeAot80);
        }

        [Theory]
        [InlineData("5%")]
        [InlineData("10ms")]
        public void CanUseStatisticalTestsToCompareFewDifferentRuntimes(string threshold)
        {
            string[] arguments = ["--runtimes", "net6.0", "net8.0", "--statisticalTest", threshold];
            var config = ConfigParser.Parse(arguments, new OutputLogger(Output)).config;
            Assert.NotNull(config);

            var mockSummary = MockFactory.CreateSummary(config);

            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.False(config.GetJobs().Last().Meta.Baseline);

            var statisticalTestColumn = config.GetColumnProviders().SelectMany(columnProvider => columnProvider.GetColumns(mockSummary))
                .OfType<StatisticalTestColumn>().Single();

            Assert.Equal(threshold, statisticalTestColumn.Threshold.ToString());
        }

        [Fact]
        public void BareStatisticalTestThresholdIsInterpretedAsNanosecondsAndWarns()
        {
            var logger = new AccumulationLogger();
            string[] arguments = ["--runtimes", "net6.0", "net8.0", "--statisticalTest", "0.02"];
            var (isSuccess, config, options) = ConfigParser.Parse(arguments, logger);

            Assert.True(isSuccess);
            Assert.NotNull(config);
            Assert.NotNull(options);

            options.StatisticalTestThreshold.Should().EndWith("ns");

            var mockSummary = MockFactory.CreateSummary(config);
            var statisticalTestColumn = config.GetColumnProviders()
                .SelectMany(columnProvider => columnProvider.GetColumns(mockSummary))
                .OfType<StatisticalTestColumn>().Single();

            statisticalTestColumn.Threshold.ToString().Should().EndWith("ns");
            logger.GetLog().Should().Contain("--statisticalTest").And.Contain("nanoseconds");
        }

        [Fact]
        public void SpecifyingInvalidStatisticalTestsThresholdMeansFailure()
        {
            Assert.False(ConfigParser.Parse(["--statisticalTest", "not a number"], new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(["--statisticalTest", "1unknownUnit"], new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(["--statisticalTest", "1 unknownUnit"], new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(["--statisticalTest", "%1"], new OutputLogger(Output)).isSuccess); // reverse order - a typo
        }

        [Fact]
        public void CanParseHardwareCounters()
        {
            var config = ConfigParser.Parse(["--counters", $"{nameof(HardwareCounter.CacheMisses)}+{nameof(HardwareCounter.InstructionRetired)}"],
                new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal(2, config.GetHardwareCounters().Count());
            Assert.Single(config.GetHardwareCounters(), counter => counter == HardwareCounter.CacheMisses);
            Assert.Single(config.GetHardwareCounters(), counter => counter == HardwareCounter.InstructionRetired);
        }

        [Fact]
        public void InvalidHardwareCounterNameMeansFailure()
        {
            Assert.False(ConfigParser.Parse(["--counters", "WRONG_NAME"], new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void TooManyHardwareCounterNameMeansFailure()
        {
            Assert.False(ConfigParser.Parse(["--counters", "Timer+TotalIssues+BranchInstructions+CacheMisses"], new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void CanParseDisassemblerWithCustomRecursiveDepth()
        {
            const int depth = 123;

            var config = ConfigParser.Parse(["--disasm", "--disasmDepth", depth.ToString()], new OutputLogger(Output)).config;
            Assert.NotNull(config);

            var diagnoser = config.GetDiagnosers().OfType<DisassemblyDiagnoser>().Single();

            Assert.Equal(depth, diagnoser.Config.MaxDepth);
        }

        [Fact]
        public void WhenCustomDisassemblerSettingsAreProvidedItsEnabledByDefault()
        {
            Verify(["--disasmDepth", "2"]);
            Verify(["--disasmFilter", "*"]);

            void Verify(string[] args)
            {
                var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;
                Assert.NotNull(config);
                Assert.Single(config.GetDiagnosers().OfType<DisassemblyDiagnoser>());
            }
        }

        [Fact]
        public void CanParseInfo()
        {
            var config = ConfigParser.Parse(["--info"], new OutputLogger(Output)).options;

            Assert.NotNull(config);
            Assert.True(config.PrintInformation);
        }

        [Fact]
        public void CanParseGnuStyleOption()
        {
            // Arrange
            var logger = new AccumulationLogger();
            var results = ConfigParser.Parse(["--inProcess=false", "--affinity=1"], logger);

            // Assert
            results.isSuccess.Should().BeTrue();
            logger.GetLog().Should().BeEmpty();
            results.options!.RunInProcess.Should().BeFalse();
            results.options!.Affinity.Should().Be(1);
        }

        [Fact]
        public void UserCanSpecifyCustomDefaultJobAndOverwriteItsSettingsViaConsoleArgs()
        {
            var globalConfig = DefaultConfig.Instance
                .AddJob(Job.Default
                    .WithWarmupCount(1)
                    .AsDefault());

            var parsedConfig = ConfigParser.Parse(["--warmupCount", "2"], new OutputLogger(Output), globalConfig).config;

            Assert.NotNull(parsedConfig);
            Assert.Equal(2, parsedConfig.GetJobs().Single().Run.WarmupCount);
            Assert.False(parsedConfig.GetJobs().Single().Meta.IsDefault); // after the merge the job is not "default" anymore
        }

        [Fact]
        public void UserCanSpecifyCustomMaxParameterColumnWidth()
        {
            const int customValue = 1234;

            var globalConfig = DefaultConfig.Instance;

            Assert.NotNull(globalConfig);
            Assert.NotNull(globalConfig.SummaryStyle);
            Assert.NotEqual(customValue, globalConfig.SummaryStyle.MaxParameterColumnWidth);

            var parsedConfig = ConfigParser.Parse(["--maxWidth", customValue.ToString()], new OutputLogger(Output), globalConfig).config;
            Assert.NotNull(parsedConfig);
            Assert.NotNull(parsedConfig.SummaryStyle);
            Assert.Equal(customValue, parsedConfig.SummaryStyle.MaxParameterColumnWidth);
        }

        [Fact]
        public void UserCanSpecifyEnvironmentVariables()
        {
            const string key = "A_VERY_NICE_ENV_VAR";
            const string value = "enabled";

            var parsedConfig = ConfigParser.Parse(["--envVars", $"{key}:{value}"], new OutputLogger(Output)).config;
            Assert.NotNull(parsedConfig);

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
            var parsedConfig = ConfigParser.Parse(["--platform", platform.ToString()], new OutputLogger(Output)).config;
            Assert.NotNull(parsedConfig);

            var job = parsedConfig.GetJobs().Single();

            var parsed = job.Environment.Platform;

            Assert.Equal(platform, parsed);
        }

        [Fact]
        public void InvalidEnvVarAreRecognized()
        {
            Assert.False(ConfigParser.Parse(["--envVars", "INVALID_NO_SEPARATOR"], new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void UserCanSpecifyNoForceGCs()
        {
            var parsedConfiguration = ConfigParser.Parse(["--noForcedGCs"], new OutputLogger(Output));
            Assert.NotNull(parsedConfiguration.config);
            Assert.True(parsedConfiguration.isSuccess);

            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                Assert.False(job.Environment.Gc.Force);
            }
        }

        [Fact]
        public void UsersCanSpecifyEvaluateOverhead()
        {
            var parsedConfiguration = ConfigParser.Parse(["--evaluateOverhead", "true"], new OutputLogger(Output));
            Assert.NotNull(parsedConfiguration.config);
            Assert.True(parsedConfiguration.isSuccess);

            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                Assert.True(job.Accuracy.EvaluateOverhead);
            }
        }

        [Fact]
        public void UsersCanSpecifyConsumeTasksSynchronously()
        {
            var parsedConfiguration = ConfigParser.Parse(["--consumeTasksSynchronously", "true"], new OutputLogger(Output));
            Assert.NotNull(parsedConfiguration.config);
            Assert.True(parsedConfiguration.isSuccess);

            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                Assert.True(job.Run.ConsumeTasksSynchronously);
            }
        }

        [Fact(Skip = "This should be handled somehow at CommandLineParser level. See https://github.com/commandlineparser/commandline/pull/892")]
        public void UserCanSpecifyWasmArgs()
        {
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "wasmnet80", "--wasmArgs", "--expose_wasm --module", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
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
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "wasmnet80", "--wasmArgs=--expose_wasm --module", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
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
            File.WriteAllLines(tempResponseFile,
            [
                "--runtimes wasmnet80",
                "--wasmArgs \"--expose_wasm --module\"",
                GetDummyWasmEngine()
            ]);
            var parsedConfiguration = ConfigParser.Parse([$"@{tempResponseFile}"], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmRuntime = Assert.IsType<WasmRuntime>(job.Environment.Runtime);
                // We may need change assertion to just "--expose_wasm --module"
                // if https://github.com/commandlineparser/commandline/pull/892 lands
                Assert.Equal(" --expose_wasm --module", wasmRuntime.JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmMainJsTemplate()
        {
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "wasmnet80", "--wasmMainJsTemplate", "./dummyFile.js", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            var job = parsedConfiguration.config!.GetJobs().Single();

            var runtime = Assert.IsType<WasmRuntime>(job.Environment.Runtime);
            Assert.Equal("dummyFile.js", runtime.MainJsTemplate?.Name);
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
            _ = ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => options.Filters = ["*"]);

            Assert.Equal(expected.Split(), updatedArgs);
        }

        [Theory]
        [InlineData("--filter abc -f abc")]
        [InlineData("--runtimes net")]
        public void CheckUpdateInvalidArgs(string strArgs)
        {
            var args = strArgs.Split();
            bool isSuccess = ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => options.Filters = ["*"]);

            Assert.Null(updatedArgs);
            Assert.False(isSuccess);
        }

        [Fact]
        public void VerifySerializeToArgs()
        {
            // Act
            var result = ConfigParser.Parse([""], NullLogger.Instance);

            // Assert
            result.isSuccess.Should().BeTrue();
            var args = ConfigParser.SerializeToArgs(result.options!);
            args.Should().BeEmpty(); // Default value is not serialized.
        }

        [Fact]
        public void VerifyDefaultValue()
        {
            // Arrange
            var logger = new AccumulationLogger();

            // Act
            var result = ConfigParser.Parse([""], logger);

            result.isSuccess.Should().BeTrue();

            var config = result.config!;
            var options = result.options!;


            // Assert

            // Verify options that has default value.
            using (var scope = new AssertionScope())
            {
                options.BaseJob.Should().Be("Default");
                options.Outliers.Should().Be(OutlierMode.RemoveUpper);
                options.ListBenchmarkCaseMode.Should().Be(ListBenchmarkCaseMode.Disabled);
                options.DisassemblerRecursiveDepth.Should().Be(1);
                options.WasmJavaScriptEngine.Should().Be("v8");
                options.WasmJavaScriptEngineArguments.Should().Be("--expose_wasm");
                options.AOTCompilerMode.Should().Be(MonoAotCompilerMode.mini);
                options.WasmRuntimeFlavor.Should().Be(RuntimeFlavor.Mono);
                options.WasmProcessTimeoutMinutes.Should().Be(10);
            }

            // Verify other options.
            using (var scope = new AssertionScope())
            {
                options.Runtimes.Should().BeEmpty();
                options.Exporters.Should().BeEmpty();
                options.UseMemoryDiagnoser.Should().BeFalse();
                options.UseThreadingDiagnoser.Should().BeFalse();
                options.UseExceptionDiagnoser.Should().BeFalse();
                options.UseDisassemblyDiagnoser.Should().BeFalse();
                options.Profiler.Should().BeEmpty();
                options.Filters.Should().BeEmpty();
                options.HiddenColumns.Should().BeEmpty();
                options.RunInProcess.Should().BeFalse();
                options.ArtifactsDirectory.Should().BeNull();
                options.Affinity.Should().BeNull();
                options.DisplayAllStatistics.Should().BeFalse();
                options.AllCategories.Should().BeEmpty();
                options.AnyCategories.Should().BeEmpty();
                options.AttributeNames.Should().BeEmpty();
                options.Join.Should().BeFalse();
                options.KeepBenchmarkFiles.Should().BeFalse();
                options.DontOverwriteResults.Should().BeFalse();
                options.HardwareCounters.Should().BeEmpty();
                options.CliPath.Should().BeNull();
                options.RestorePath.Should().BeNull();
                options.RestorePath.Should().BeNull();
                options.CoreRunPaths.Should().BeEmpty();
                options.MonoPath.Should().BeNull();
                options.ClrVersion.Should().BeEmpty();
                options.ILCompilerVersion.Should().BeEmpty();
                options.IlcPackages.Should().BeNull();
                options.LaunchCount.Should().BeNull();
                options.WarmupIterationCount.Should().BeNull();
                options.MinWarmupIterationCount.Should().BeNull();
                options.MaxWarmupIterationCount.Should().BeNull();
                options.IterationTimeInMilliseconds.Should().BeNull();
                options.IterationCount.Should().BeNull();
                options.MinIterationCount.Should().BeNull();
                options.MaxIterationCount.Should().BeNull();
                options.InvocationCount.Should().BeNull();
                options.UnrollFactor.Should().BeNull();
                options.RunStrategy.Should().BeNull();
                options.RunOncePerIteration.Should().BeFalse();
                options.PrintInformation.Should().BeFalse();
                options.ApplesToApples.Should().BeFalse();
                options.DisassemblerFilters.Should().BeEmpty();
                options.DisassemblerDiff.Should().BeFalse();
                options.LogBuildOutput.Should().BeFalse();
                options.GenerateMSBuildBinLog.Should().BeFalse();
                options.TimeOutInSeconds.Should().BeNull();
                options.WakeLock.Should().BeNull();
                options.StopOnFirstError.Should().BeFalse();
                options.StatisticalTestThreshold.Should().BeEmpty();
                options.DisableLogFile.Should().BeFalse();
                options.MaxParameterColumnWidth.Should().BeNull();
                options.EnvironmentVariables.Should().BeEmpty();
                options.MemoryRandomization.Should().BeFalse();
                options.WasmMainJsTemplate.Should().BeNull();
                options.CustomRuntimePack.Should().BeEmpty();
                options.AOTCompilerPath.Should().BeNull();
                options.NoForcedGCs.Should().BeFalse();
                options.EvaluateOverhead.Should().BeFalse();
                options.Resume.Should().BeFalse();
            }
        }

        [Fact]
        public void VerifyHelpMessage()
        {
            // Arrange
            var logger = new AccumulationLogger();

            // Act
            bool isSuccess = ConfigParser.Parse(["--help"], logger).isSuccess;

            // Assert
            isSuccess.Should().BeTrue();
            var helpMessage = logger.GetLog().Trim();

            // TODO: Remove temporary workaround code after migrated to xUnit v3
            var exeName = "BenchmarkDotNet.Tests";
            helpMessage = helpMessage.Replace("  testhost.net472.arm64", $"  {exeName}")
                                     .Replace("  testhost.net472", $"  {exeName}")
                                     .Replace("  testhost", $"  {exeName}");

            helpMessage.Should().BeEquivalentTo(
                """
                Description:
                  BenchmarkDotNet Command Line options

                Usage:
                  BenchmarkDotNet.Tests [options]

                Options:
                  -j, --job <job>                                                                     Dry/Short/Medium/Long or Default [default: Default]
                  -r, --runtimes <runtimes>                                                           Full target framework moniker for .NET Core and .NET. For Mono just 'Mono'. For NativeAOT please append target runtime version (example: 'nativeaot7.0'). First one will be marked as baseline!
                  -e, --exporters <exporters>                                                         GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML/CSVMeasurements/Markdown/Atlassian/Plain/BriefJSON/FullJSON/Asciidoc/BriefXML/FullXML/OpenMetrics
                  -m, --memory                                                                        Prints memory statistics
                  -t, --threading                                                                     Prints threading statistics
                  --exceptions                                                                        Prints exception statistics
                  -d, --disasm                                                                        Gets disassembly of benchmarked code
                  -p, --profiler <profiler>                                                           Profiles benchmarked code using selected profiler. Available options: EP/ETW/CV/NativeMemory
                  -f, --filter <filter>                                                               Glob patterns
                  -h, --hide <hide>                                                                   Hides columns by name
                  -i, --inProcess                                                                     Run benchmarks in Process
                  -a, --artifacts <artifacts>                                                         Valid path to accessible directory
                  --outliers <DontRemove|RemoveAll|RemoveLower|RemoveUpper>                           DontRemove/RemoveUpper/RemoveLower/RemoveAll [default: RemoveUpper]
                  --affinity <affinity>                                                               Affinity mask to set for the benchmark process
                  --allStats                                                                          Displays all statistics (min, max & more)
                  --allCategories <allCategories>                                                     Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed
                  --anyCategories <anyCategories>                                                     Any Categories to run
                  --attribute <attribute>                                                             Run all methods with given attribute (applied to class or method)
                  --join                                                                              Prints single table with results for all benchmarks
                  --keepFiles                                                                         Determines if all auto-generated files should be kept or removed after running the benchmarks.
                  --noOverwrite                                                                       Determines if the exported result files should not be overwritten (by default they are overwritten).
                  --counters <counters>                                                               Hardware Counters
                  --cli <cli>                                                                         Path to dotnet cli (optional).
                  --packages <packages>                                                               The directory to restore packages to (optional).
                  --coreRun <coreRun>                                                                 Path(s) to CoreRun (optional).
                  --monoPath <monoPath>                                                               Optional path to Mono which should be used for running benchmarks.
                  --clrVersion <clrVersion>                                                           Optional version of private CLR build used as the value of COMPLUS_Version env var.
                  --ilCompilerVersion <ilCompilerVersion>                                             Optional version of Microsoft.DotNet.ILCompiler which should be used to run with NativeAOT. Example: "7.0.0-preview.3.22123.2"
                  --ilcPackages <ilcPackages>                                                         Optional path to shipping packages produced by local dotnet/runtime build.
                  --launchCount <launchCount>                                                         How many times we should launch process with target benchmark. The default is 1.
                  --warmupCount <warmupCount>                                                         How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.
                  --minWarmupCount <minWarmupCount>                                                   Minimum count of warmup iterations that should be performed. The default is 6.
                  --maxWarmupCount <maxWarmupCount>                                                   Maximum count of warmup iterations that should be performed. The default is 50.
                  --iterationTime <iterationTime>                                                     Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default
                  --iterationCount <iterationCount>                                                   How many target iterations should be performed. By default calculated by the heuristic.
                  --minIterationCount <minIterationCount>                                             Minimum number of iterations to run. The default is 15.
                  --maxIterationCount <maxIterationCount>                                             Maximum number of iterations to run. The default is 100.
                  --invocationCount <invocationCount>                                                 Invocation count in a single iteration. By default calculated by the heuristic.
                  --unrollFactor <unrollFactor>                                                       How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default
                  --strategy <ColdStart|Monitoring|Throughput>                                        The RunStrategy that should be used. Throughput/ColdStart/Monitoring.
                  --platform <AnyCpu|Arm|Arm64|Armv6|LoongArch64|Ppc64le|RiscV64|S390x|Wasm|X64|X86>  The Platform that should be used. If not specified, the host process platform is used (default). AnyCpu/X86/X64/Arm/Arm64/LoongArch64.
                  --runOncePerIteration                                                               Run the benchmark exactly once per iteration.
                  --info                                                                              Print environment information.
                  --apples                                                                            Runs apples-to-apples comparison for specified Jobs.
                  --list <Disabled|Flat|Tree>                                                         Prints all of the available benchmark names. Flat/Tree [default: Disabled]
                  --disasmDepth <disasmDepth>                                                         Sets the recursive depth for the disassembler. [default: 1]
                  --disasmFilter <disasmFilter>                                                       Glob patterns applied to full method signatures by the disassembler.
                  --disasmDiff                                                                        Generates diff reports for the disassembler.
                  --logBuildOutput                                                                    Log Build output.
                  --generateBinLog                                                                    Generate msbuild binlog for builds
                  --buildTimeout <buildTimeout>                                                       Build timeout in seconds.
                  --wakeLock <Display|None|System>                                                    Prevents the system from entering sleep or turning off the display. None/System/Display.
                  --stopOnFirstError                                                                  Stop on first error.
                  --statisticalTest <statisticalTest>                                                 Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s. Bare numbers imply ns (e.g. 0.02 -> 0.02ns)
                  --disableLogFile                                                                    Disables the logfile.
                  --maxWidth <maxWidth>                                                               Max parameter column width, the default is 20.
                  --envVars <envVars>                                                                 Colon separated environment variables (key:value)
                  --memoryRandomization                                                               Specifies whether Engine should allocate some random-sized memory between iterations.
                  --wasmEngine <wasmEngine>                                                           Specifies the executable (in PATH) or full path to a java script engine used to run the benchmarks, used by Wasm toolchain. [default: v8]
                  --wasmArgs <wasmArgs>                                                               Arguments for the javascript engine used by Wasm toolchain. [default: --expose_wasm]
                  --wasmMainJsTemplate <wasmMainJsTemplate>                                           Path to main.mjs template.
                  --customRuntimePack <customRuntimePack>                                             Path to a custom runtime pack. Only used for wasm/MonoAotLLVM currently.
                  --AOTCompilerPath <AOTCompilerPath>                                                 Path to Mono AOT compiler, used for MonoAotLLVM.
                  --AOTCompilerMode <llvm|mini|wasm>                                                  Mono AOT compiler mode, either 'mini' or 'llvm' [default: mini]
                  --wasmRuntimeFlavor <CoreCLR|Mono>                                                  Runtime flavor for WASM benchmarks: 'Mono' (default) uses the Mono runtime pack, 'CoreCLR' uses the CoreCLR runtime pack. [default: Mono]
                  --wasmProcessTimeout <wasmProcessTimeout>                                           Maximum time in minutes to wait for a single WASM benchmark process to finish before force killing it. [default: 10]
                  --noForcedGCs                                                                       Specifying would not forcefully induce any GCs.
                  --evaluateOverhead                                                                  Specifying would not run the evaluation overhead iterations.
                  --resume                                                                            Continue the execution if the last run was stopped.
                  -?, -h, --help                                                                      Show help and usage information
                  --version                                                                           Show version information
                """,
                o => o.IgnoringNewlineStyle());
        }

        private string GetDummyWasmEngine()
        {
            // We know, that this file exists, that's enough.
            return $"--wasmEngine={Assembly.GetExecutingAssembly().Location}";
        }
    }
}
