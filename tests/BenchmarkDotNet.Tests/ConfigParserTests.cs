using AwesomeAssertions;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
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
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.Wasm;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.Framework;
using Perfolizer.Horology;
using System.Reflection;
using BenchmarkDotNet.Toolchains.NetCoreApp;

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
        public void CustomExporterIsResolvedFromAssemblyQualifiedTypeName()
        {
            var (isSuccess, config, _) = ConfigParser.Parse(["--exporters", typeof(CustomExporter).AssemblyQualifiedName!], new OutputLogger(Output));

            Assert.True(isSuccess);
            Assert.NotNull(config);
            Assert.Single(config.GetExporters());
            Assert.IsType<CustomExporter>(config.GetExporters().Single());
        }

        [Fact]
        public void BuiltInAndCustomExportersCanBeCombined()
        {
            var (isSuccess, config, _) = ConfigParser.Parse(["--exporters", "json", typeof(CustomExporter).AssemblyQualifiedName!], new OutputLogger(Output));

            Assert.True(isSuccess);
            Assert.NotNull(config);
            Assert.Contains(JsonExporter.Default, config.GetExporters());
            Assert.Contains(config.GetExporters(), e => e is CustomExporter);
        }

        [Fact]
        public void UnknownExporterNameFailsParsing()
        {
            var (isSuccess, config, _) = ConfigParser.Parse(["--exporters", "does-not-exist"], new OutputLogger(Output));

            Assert.False(isSuccess);
            Assert.Null(config);
        }

        [Fact]
        public void CustomExporterThatDoesNotImplementIExporterFailsParsing()
        {
            var (isSuccess, config, _) = ConfigParser.Parse(["--exporters", typeof(NotAnExporter).AssemblyQualifiedName!], new OutputLogger(Output));

            Assert.False(isSuccess);
            Assert.Null(config);
        }

        [Fact]
        public void CustomExporterWithoutParameterlessConstructorFailsParsing()
        {
            var (isSuccess, config, _) = ConfigParser.Parse(["--exporters", typeof(ExporterWithoutParameterlessCtor).AssemblyQualifiedName!], new OutputLogger(Output));

            Assert.False(isSuccess);
            Assert.Null(config);
        }

        public class CustomExporter : ExporterBase
        {
            public override ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken) => new();
        }

        public class NotAnExporter { }

        public class ExporterWithoutParameterlessCtor : ExporterBase
        {
            public ExporterWithoutParameterlessCtor(int _) { }

            public override ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken) => new();
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
            Assert.Equal(((Runtime)RuntimeInformation.GetCurrentRuntime()).GetTfm(),
                ((DotNetCliGenerator)toolchain.Generator).Settings.TargetFrameworkMoniker); // runtime was not specified so the current was used
            Assert.Equal(fakeCoreRunPath, toolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, toolchain.Settings.CliPath?.FullName);
            Assert.Equal(fakeRestorePackages, toolchain.Settings.PackagesPath?.FullName);
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
            Assert.Equal("net11.0", generator.Settings.TargetFrameworkMoniker);
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
            Assert.Equal(((Runtime)RuntimeInformation.GetCurrentRuntime()).GetTfm(), generator.Settings.TargetFrameworkMoniker);
            Assert.Equal(fakeCoreRunPath, coreRunToolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, coreRunToolchain.Settings.CliPath?.FullName);
            Assert.Equal(fakeRestorePackages, coreRunToolchain.Settings.PackagesPath?.FullName);

            CsProjCoreToolchain coreToolchain = (CsProjCoreToolchain)runtimeJob.GetToolchain();
            generator = (DotNetCliGenerator)coreToolchain.Generator;
            Assert.Equal(runtime, ((DotNetCliGenerator)coreToolchain.Generator).Settings.TargetFrameworkMoniker);
            Assert.Equal(fakeDotnetCliPath, coreToolchain.Settings.CliPath?.FullName);
            Assert.Equal(fakeRestorePackages, generator.Settings.PackagesPath?.FullName);
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
            Assert.Equal(runtime1, ((DotNetCliGenerator)baselineJob.GetToolchain().Generator).Settings.TargetFrameworkMoniker);
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
            var toolchain = Assert.IsType<RoslynMonoToolchain>(config.GetJobs().Single().GetToolchain());
            Assert.IsType<MonoRuntime>(toolchain.Runtime);
            Assert.Equal(fakeMonoPath, toolchain.Settings.MonoPath?.FullName);
        }

        [Fact]
        public void MonoAotIsParsedAndMonoPathHonored()
        {
            var fakeMonoPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(["-r", "monoaot", "--monoPath", fakeMonoPath], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            var toolchain = Assert.IsType<RoslynMonoAotToolchain>(config.GetJobs().Single().GetToolchain());
            Assert.IsType<MonoAotRuntime>(toolchain.Runtime);
            Assert.Equal(fakeMonoPath, toolchain.Settings.MonoPath?.FullName);
        }

        [Theory]
        [InlineData("monowasm8.0", typeof(CsProjMonoWasmToolchain))]
        [InlineData("monowasmaot8.0", typeof(CsProjMonoWasmAotToolchain))]
        [InlineData("corewasm11.0", typeof(CsProjCoreWasmToolchain))]
        public void WasmRuntimesResolveToTheirToolchains(string moniker, Type expectedToolchain)
        {
            var config = ConfigParser.Parse(["-r", moniker, GetDummyWasmEngine()], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.IsType(expectedToolchain, config.GetJobs().Single().GetToolchain());
        }

        [Fact]
        public void IlCompilerPathParsedCorrectly()
        {
            var fakePath = new FileInfo(typeof(ConfigParserTests).Assembly.Location).Directory!;
            var config = ConfigParser.Parse(["-r", "nativeaot10.0", "--ilcPackages", fakePath.FullName], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            CsProjNativeAotToolchain? toolchain = config.GetJobs().Single().GetToolchain() as CsProjNativeAotToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakePath.FullName, ((NativeAotSettings)toolchain.Settings).LocalIlcPackages?.FullName);
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
                Assert.Equal(fakeDotnetCliPath, ((CsProjCoreToolchain)toolchain).Settings.CliPath?.FullName);
            }
            else
            {
                Assert.True(toolchain is CsProjFrameworkToolchain);
                Assert.Equal(fakeDotnetCliPath, ((DotNetCliBuilder)toolchain.Builder).CustomDotNetCliPath?.FullName);
            }
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).Settings.TargetFrameworkMoniker);
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
        public void TitleParsedCorrectly()
        {
            var config = ConfigParser.Parse(["--title", "MyCustomTitle"], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Equal("MyCustomTitle", config.Title);
        }

        [Fact]
        public void WhenTitleIsNotSpecifiedItIsNull()
        {
            var config = ConfigParser.Parse([], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Null(config.Title);
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
            Assert.Equal(fakeRestoreDirectory, ((DotNetCliGenerator)toolchain.Generator).Settings.PackagesPath?.FullName);
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
        [InlineData("net461", "4.6.1")]
        [InlineData("net462", "4.6.2")]
        [InlineData("net47", "4.7")]
        [InlineData("net471", "4.7.1")]
        [InlineData("net472", "4.7.2")]
        [InlineData("net48", "4.8")]
        [InlineData("net481", "4.8.1")]
        public void NetFrameworkMonikerParsedCorrectly(string tfm, string expectedVersion)
        {
            var config = ConfigParser.Parse(["-r", tfm], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            // A netfx moniker only sets the runtime; the default toolchain is auto-selected and becomes the faster
            // Roslyn toolchain when the requested version matches the host, so assert on the runtime, not the toolchain.
            var runtime = Assert.IsType<ClrRuntime>(config.GetJobs().Single().GetRuntime());
            Assert.Equal(Version.Parse(expectedVersion), runtime.Version);
        }

        [Theory]
        [InlineData("net50", "net5.0")]
        [InlineData("net5.0", "net5.0")]
        [InlineData("net60", "net6.0")]
        [InlineData("net6.0", "net6.0")]
        [InlineData("net70", "net7.0")]
        [InlineData("net7.0", "net7.0")]
        [InlineData("net80", "net8.0")]
        [InlineData("net8.0", "net8.0")]
        [InlineData("net90", "net9.0")]
        [InlineData("net9.0", "net9.0")]
        [InlineData("net10_0", "net10.0")]
        [InlineData("net10.0", "net10.0")]
        [InlineData("net11_0", "net11.0")]
        [InlineData("net11.0", "net11.0")]
        public void NetMonikersAreRecognizedAsNetCoreMonikers(string tfm, string expectedTfm)
        {
            var config = ConfigParser.Parse(["-r", tfm], new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.Single(config.GetJobs());
            var toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(expectedTfm, ((DotNetCliGenerator)toolchain.Generator).Settings.TargetFrameworkMoniker);
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
            Assert.Equal(msBuildMoniker, ((DotNetCliGenerator)toolchain.Generator).Settings.TargetFrameworkMoniker);
        }

        [Fact]
        public void CanCompareFewDifferentRuntimes()
        {
            var config = ConfigParser.Parse(["--runtimes", "net462", "MONO", "netcoreapp2.0", "nativeaot8.0", "nativeAOT9.0", "nativeAOT10.0"],
                new OutputLogger(Output)).config;

            Assert.NotNull(config);
            Assert.True(config.GetJobs().First().Meta.Baseline); // when the user provides multiple runtimes the first one should be marked as baseline
            Assert.Single(config.GetJobs(), job => job.GetRuntime() is ClrRuntime clrRuntime && clrRuntime.Version == new Version(4, 6, 2));
            Assert.Single(config.GetJobs(), job => job.GetRuntime() is MonoRuntime);
            Assert.Single(config.GetJobs(), job =>
                job.GetRuntime() is CoreRuntime coreRuntime && coreRuntime.Version == new Version(2, 0));
            Assert.Single(config.GetJobs(), job =>
                job.GetRuntime() is NativeAotRuntime nativeAot && nativeAot.Version == new Version(8, 0));
            Assert.Single(config.GetJobs(), job =>
                job.GetRuntime() is NativeAotRuntime nativeAot && nativeAot.Version == new Version(9, 0));
            Assert.Single(config.GetJobs(), job =>
                job.GetRuntime() is NativeAotRuntime nativeAot && nativeAot.Version == new Version(10, 0));
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
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "monowasm80", "--wasmArgs", "--expose_wasm --module", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmToolchain = Assert.IsAssignableFrom<CsProjWasmToolchain>(job.GetToolchain());
                Assert.Equal(" --expose_wasm --module", ((WasmSettings)wasmToolchain.Settings).JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmArgsUsingEquals()
        {
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "monowasm80", "--wasmArgs=--expose_wasm --module", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmToolchain = Assert.IsAssignableFrom<CsProjWasmToolchain>(job.GetToolchain());
                Assert.Equal("--expose_wasm --module", ((WasmSettings)wasmToolchain.Settings).JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmArgsViaResponseFile()
        {
            var tempResponseFile = Path.GetRandomFileName();
            File.WriteAllLines(tempResponseFile,
            [
                "--runtimes monowasm80",
                "--wasmArgs \"--expose_wasm --module\"",
                GetDummyWasmEngine()
            ]);
            var parsedConfiguration = ConfigParser.Parse([$"@{tempResponseFile}"], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            Assert.NotNull(parsedConfiguration.config);
            var jobs = parsedConfiguration.config.GetJobs();
            foreach (var job in parsedConfiguration.config.GetJobs())
            {
                var wasmToolchain = Assert.IsAssignableFrom<CsProjWasmToolchain>(job.GetToolchain());
                // We may need change assertion to just "--expose_wasm --module"
                // if https://github.com/commandlineparser/commandline/pull/892 lands
                Assert.Equal(" --expose_wasm --module", ((WasmSettings)wasmToolchain.Settings).JavaScriptEngineArguments);
            }
        }

        [Fact]
        public void UserCanSpecifyWasmMainJsTemplate()
        {
            var parsedConfiguration = ConfigParser.Parse(["--runtimes", "monowasm80", "--wasmMainJsTemplate", "./dummyFile.js", GetDummyWasmEngine()], new OutputLogger(Output));
            Assert.True(parsedConfiguration.isSuccess);
            var job = parsedConfiguration.config!.GetJobs().Single();

            var wasmToolchain = Assert.IsAssignableFrom<CsProjWasmToolchain>(job.GetToolchain());
            Assert.Equal("dummyFile.js", ((WasmSettings)wasmToolchain.Settings).MainJsTemplate?.Name);
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

        private string GetDummyWasmEngine()
        {
            // We know, that this file exists, that's enough.
            return $"--wasmEngine={Assembly.GetExecutingAssembly().Location}";
        }
    }
}
