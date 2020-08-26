using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.MonoWasm;
using CommandLine;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace BenchmarkDotNet.ConsoleArguments
{
    public static class ConfigParser
    {
        private const int MinimumDisplayWidth = 80;
        private const char EnvVarKeyValueSeparator = ':';

        private static readonly IReadOnlyDictionary<string, Job> AvailableJobs = new Dictionary<string, Job>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "default", Job.Default },
            { "dry", Job.Dry },
            { "short", Job.ShortRun },
            { "medium", Job.MediumRun },
            { "long", Job.LongRun },
            { "verylong", Job.VeryLongRun }
        };

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        private static readonly IReadOnlyDictionary<string, IExporter[]> AvailableExporters =
            new Dictionary<string, IExporter[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "csv", new[] { CsvExporter.Default } },
                { "csvmeasurements", new[] { CsvMeasurementsExporter.Default } },
                { "html", new[] { HtmlExporter.Default } },
                { "markdown", new[] { MarkdownExporter.Default } },
                { "atlassian", new[] { MarkdownExporter.Atlassian } },
                { "stackoverflow", new[] { MarkdownExporter.StackOverflow } },
                { "github", new[] { MarkdownExporter.GitHub } },
                { "plain", new[] { PlainExporter.Default } },
                { "rplot", new[] { CsvMeasurementsExporter.Default, RPlotExporter.Default } }, // R Plots depends on having the full measurements available
                { "json", new[] { JsonExporter.Default } },
                { "briefjson", new[] { JsonExporter.Brief } },
                { "fulljson", new[] { JsonExporter.Full } },
                { "asciidoc", new[] { AsciiDocExporter.Default } },
                { "xml", new[] { XmlExporter.Default } },
                { "briefxml", new[] { XmlExporter.Brief } },
                { "fullxml", new[] { XmlExporter.Full } }
            };

        public static (bool isSuccess, IConfig config, CommandLineOptions options) Parse(string[] args, ILogger logger, IConfig globalConfig = null)
        {
            (bool isSuccess, IConfig config, CommandLineOptions options) result = default;

            using (var parser = CreateParser(logger))
            {
                parser
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsed(options => result = Validate(options, logger) ? (true, CreateConfig(options, globalConfig), options) : (false, default, default))
                    .WithNotParsed(errors => result = (false, default, default));
            }

            return result;
        }

        private static Parser CreateParser(ILogger logger)
            => new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = new LoggerWrapper(logger);
                settings.MaximumDisplayWidth = Math.Max(MinimumDisplayWidth, GetMaximumDisplayWidth());
            });

        private static bool Validate(CommandLineOptions options, ILogger logger)
        {
            if (!AvailableJobs.ContainsKey(options.BaseJob))
            {
                logger.WriteLineError($"The provided base job \"{options.BaseJob}\" is invalid. Available options are: {string.Join(", ", AvailableJobs.Keys)}.");
                return false;
            }

            foreach (string runtime in options.Runtimes)
            {
                if (!Enum.TryParse<RuntimeMoniker>(runtime.Replace(".", string.Empty), ignoreCase: true, out var parsed))
                {
                    logger.WriteLineError($"The provided runtime \"{runtime}\" is invalid. Available options are: {string.Join(", ", Enum.GetNames(typeof(RuntimeMoniker)).Select(name => name.ToLower()))}.");
                    return false;
                }
                else if (parsed == RuntimeMoniker.Wasm && (options.WasmMainJs == null || options.WasmMainJs.IsNotNullButDoesNotExist()))
                {
                    logger.WriteLineError($"The provided {nameof(options.WasmMainJs)} \"{options.WasmMainJs}\" does NOT exist. It MUST be provided.");
                    return false;
                }
            }

            foreach (string exporter in options.Exporters)
                if (!AvailableExporters.ContainsKey(exporter))
                {
                    logger.WriteLineError($"The provided exporter \"{exporter}\" is invalid. Available options are: {string.Join(", ", AvailableExporters.Keys)}.");
                    return false;
                }

            if (options.CliPath.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.CliPath)} \"{options.CliPath}\" does NOT exist.");
                return false;
            }

            foreach (var coreRunPath in options.CoreRunPaths)
                if (coreRunPath.IsNotNullButDoesNotExist())
                {
                    logger.WriteLineError($"The provided path to CoreRun: \"{coreRunPath}\" does NOT exist. Please remember that BDN expects path to CoreRun.exe (corerun on Unix), not to Core_Root folder.");
                    return false;
                }

            if (options.MonoPath.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.MonoPath)} \"{options.MonoPath}\" does NOT exist.");
                return false;
            }

            if (options.WasmJavascriptEngine.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.WasmJavascriptEngine)} \"{options.WasmJavascriptEngine}\" does NOT exist.");
                return false;
            }

            if (options.CoreRtPath.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.CoreRtPath)} \"{options.CoreRtPath}\" does NOT exist.");
                return false;
            }

            if (options.Runtimes.Count() > 1 && !options.CoreRunPaths.IsNullOrEmpty())
            {
                logger.WriteLineError("CoreRun path can't be combined with multiple .NET Runtimes");
                return false;
            }

            if (options.HardwareCounters.Count() > 3)
            {
                logger.WriteLineError("You can't use more than 3 HardwareCounters at the same time.");
                return false;
            }

            foreach (var counterName in options.HardwareCounters)
                if (!Enum.TryParse(counterName, ignoreCase: true, out HardwareCounter _))
                {
                    logger.WriteLineError($"The provided hardware counter \"{counterName}\" is invalid. Available options are: {string.Join("+", Enum.GetNames(typeof(HardwareCounter)))}.");
                    return false;
                }

            if (!string.IsNullOrEmpty(options.StatisticalTestThreshold) && !Threshold.TryParse(options.StatisticalTestThreshold, out _))
            {
                logger.WriteLineError("Invalid Threshold for Statistical Test. Use --help to see examples.");
                return false;
            }

            if (options.EnvironmentVariables.Any(envVar => envVar.IndexOf(EnvVarKeyValueSeparator) <= 0))
            {
                logger.WriteLineError($"Environment variable value must be separated from the key using '{EnvVarKeyValueSeparator}'. Use --help to see examples.");
                return false;
            }

            return true;
        }

        private static IConfig CreateConfig(CommandLineOptions options, IConfig globalConfig)
        {
            var config = new ManualConfig();

            var baseJob = GetBaseJob(options, globalConfig);
            var expanded = Expand(baseJob.UnfreezeCopy(), options).ToArray(); // UnfreezeCopy ensures that each of the expanded jobs will have it's own ID
            if (expanded.Length > 1)
                expanded[0] = expanded[0].AsBaseline(); // if the user provides multiple jobs, then the first one should be a baseline
            config.AddJob(expanded);
            if (config.GetJobs().IsEmpty() && baseJob != Job.Default)
                config.AddJob(baseJob);

            config.AddExporter(options.Exporters.SelectMany(exporter => AvailableExporters[exporter]).ToArray());

            config.AddHardwareCounters(options.HardwareCounters
                .Select(counterName => (HardwareCounter)Enum.Parse(typeof(HardwareCounter), counterName, ignoreCase: true))
                .ToArray());

            if (options.UseMemoryDiagnoser)
                config.AddDiagnoser(MemoryDiagnoser.Default);
            if (options.UseThreadingDiagnoser)
                config.AddDiagnoser(ThreadingDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: options.DisassemblerRecursiveDepth, exportDiff: options.DisassemblerDiff)));
            if (!string.IsNullOrEmpty(options.Profiler))
                config.AddDiagnoser(DiagnosersLoader.GetImplementation<IProfiler>(profiler => profiler.ShortName.EqualsWithIgnoreCase(options.Profiler)));

            if (options.DisplayAllStatistics)
                config.AddColumn(StatisticColumn.AllStatistics);
            if (!string.IsNullOrEmpty(options.StatisticalTestThreshold) && Threshold.TryParse(options.StatisticalTestThreshold, out var threshold))
                config.AddColumn(new StatisticalTestColumn(StatisticalTestKind.MannWhitney, threshold));

            if (options.ArtifactsDirectory != null)
                config.ArtifactsPath = options.ArtifactsDirectory.FullName;

            var filters = GetFilters(options).ToArray();
            if (filters.Length > 1)
                config.AddFilter(new UnionFilter(filters));
            else
                config.AddFilter(filters);

            config.WithOption(ConfigOptions.JoinSummary, options.Join);
            config.WithOption(ConfigOptions.KeepBenchmarkFiles, options.KeepBenchmarkFiles);
            config.WithOption(ConfigOptions.DontOverwriteResults, options.DontOverwriteResults);
            config.WithOption(ConfigOptions.StopOnFirstError, options.StopOnFirstError);
            config.WithOption(ConfigOptions.DisableLogFile, options.DisableLogFile);

            if (options.MaxParameterColumnWidth.HasValue)
                config.WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(options.MaxParameterColumnWidth.Value));

            return config;
        }

        private static Job GetBaseJob(CommandLineOptions options, IConfig globalConfig)
        {
            var baseJob =
                globalConfig?.GetJobs().SingleOrDefault(job => job.Meta.IsDefault) // global config might define single custom Default job
                ?? AvailableJobs[options.BaseJob.ToLowerInvariant()];

            if (baseJob != Job.Dry && options.Outliers != OutlierMode.RemoveUpper)
                baseJob = baseJob.WithOutlierMode(options.Outliers);

            if (options.Affinity.HasValue)
                baseJob = baseJob.WithAffinity((IntPtr) options.Affinity.Value);

            if (options.LaunchCount.HasValue)
                baseJob = baseJob.WithLaunchCount(options.LaunchCount.Value);
            if (options.WarmupIterationCount.HasValue)
                baseJob = baseJob.WithWarmupCount(options.WarmupIterationCount.Value);
            if (options.MinWarmupIterationCount.HasValue)
                baseJob = baseJob.WithMinWarmupCount(options.MinWarmupIterationCount.Value);
            if (options.MaxWarmupIterationCount.HasValue)
                baseJob = baseJob.WithMaxWarmupCount(options.MaxWarmupIterationCount.Value);
            if (options.IterationTimeInMilliseconds.HasValue)
                baseJob = baseJob.WithIterationTime(TimeInterval.FromMilliseconds(options.IterationTimeInMilliseconds.Value));
            if (options.IterationCount.HasValue)
                baseJob = baseJob.WithIterationCount(options.IterationCount.Value);
            if (options.MinIterationCount.HasValue)
                baseJob = baseJob.WithMinIterationCount(options.MinIterationCount.Value);
            if (options.MaxIterationCount.HasValue)
                baseJob = baseJob.WithMaxIterationCount(options.MaxIterationCount.Value);
            if (options.InvocationCount.HasValue)
                baseJob = baseJob.WithInvocationCount(options.InvocationCount.Value);
            if (options.UnrollFactor.HasValue)
                baseJob = baseJob.WithUnrollFactor(options.UnrollFactor.Value);
            if (options.RunStrategy.HasValue)
                baseJob = baseJob.WithStrategy(options.RunStrategy.Value);
            if (options.Platform.HasValue)
                baseJob = baseJob.WithPlatform(options.Platform.Value);
            if (options.RunOncePerIteration)
                baseJob = baseJob.RunOncePerIteration();

            if (options.EnvironmentVariables.Any())
            {
                baseJob = baseJob.WithEnvironmentVariables(options.EnvironmentVariables.Select(text =>
                {
                    var separated = text.Split(new[] { EnvVarKeyValueSeparator }, 2);
                    return new EnvironmentVariable(separated[0], separated[1]);
                }).ToArray());
            }

            if (AvailableJobs.Values.Contains(baseJob)) // no custom settings
                return baseJob;

            return baseJob
                .AsDefault(false) // after applying all settings from console args the base job is not default anymore
                .AsMutator(); // we mark it as mutator so it will be applied to other jobs defined via attributes and merged later in GetRunnableJobs method
        }

        private static IEnumerable<Job> Expand(Job baseJob, CommandLineOptions options)
        {
            if (options.RunInProcess)
                yield return baseJob.WithToolchain(InProcessEmitToolchain.Instance);
            else if (!string.IsNullOrEmpty(options.ClrVersion))
                yield return baseJob.WithRuntime(ClrRuntime.CreateForLocalFullNetFrameworkBuild(options.ClrVersion)); // local builds of .NET Runtime
            else if (options.CoreRunPaths.Any())
                foreach (var coreRunPath in options.CoreRunPaths)
                    yield return CreateCoreRunJob(baseJob, options, coreRunPath); // local CoreFX and CoreCLR builds
            else if (options.CliPath != null && options.Runtimes.IsEmpty())
                yield return CreateCoreJobWithCli(baseJob, options);
            else
                foreach (string runtime in options.Runtimes) // known runtimes
                    yield return CreateJobForGivenRuntime(baseJob, runtime, options);
        }

        private static Job CreateJobForGivenRuntime(Job baseJob, string runtimeId, CommandLineOptions options)
        {
            TimeSpan? timeOut = options.TimeOutInSeconds.HasValue ? TimeSpan.FromSeconds(options.TimeOutInSeconds.Value) : default(TimeSpan?);

            if (!Enum.TryParse(runtimeId.Replace(".", string.Empty), ignoreCase: true, out RuntimeMoniker runtimeMoniker))
            {
                throw new InvalidOperationException("Impossible, already validated by the Validate method");
            }

            switch (runtimeMoniker)
            {
                case RuntimeMoniker.Net461:
                case RuntimeMoniker.Net462:
                case RuntimeMoniker.Net47:
                case RuntimeMoniker.Net471:
                case RuntimeMoniker.Net472:
                case RuntimeMoniker.Net48:
                    return baseJob
                        .WithRuntime(runtimeMoniker.GetRuntime())
                        .WithToolchain(CsProjClassicNetToolchain.From(runtimeId, options.RestorePath?.FullName, timeOut));
                case RuntimeMoniker.NetCoreApp20:
                case RuntimeMoniker.NetCoreApp21:
                case RuntimeMoniker.NetCoreApp22:
                case RuntimeMoniker.NetCoreApp30:
                case RuntimeMoniker.NetCoreApp31:
#pragma warning disable CS0618 // Type or member is obsolete
                case RuntimeMoniker.NetCoreApp50:
#pragma warning restore CS0618 // Type or member is obsolete
                case RuntimeMoniker.Net50:
                case RuntimeMoniker.Net60:
                    return baseJob
                        .WithRuntime(runtimeMoniker.GetRuntime())
                        .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings(runtimeId, null, runtimeId, options.CliPath?.FullName, options.RestorePath?.FullName, timeOut)));
                case RuntimeMoniker.Mono:
                    return baseJob.WithRuntime(new MonoRuntime("Mono", options.MonoPath?.FullName));
                case RuntimeMoniker.CoreRt20:
                case RuntimeMoniker.CoreRt21:
                case RuntimeMoniker.CoreRt22:
                case RuntimeMoniker.CoreRt30:
                case RuntimeMoniker.CoreRt31:
                case RuntimeMoniker.CoreRt50:
                case RuntimeMoniker.CoreRt60:
                    var builder = CoreRtToolchain.CreateBuilder();

                    if (options.CliPath != null)
                        builder.DotNetCli(options.CliPath.FullName);
                    if (options.RestorePath != null)
                        builder.PackagesRestorePath(options.RestorePath.FullName);

                    if (options.CoreRtPath != null)
                        builder.UseCoreRtLocal(options.CoreRtPath.FullName);
                    else if (!string.IsNullOrEmpty(options.CoreRtVersion))
                        builder.UseCoreRtNuGet(options.CoreRtVersion);
                    else
                        builder.UseCoreRtNuGet();

                    if (timeOut.HasValue)
                        builder.Timeout(timeOut.Value);

                    var runtime = runtimeMoniker.GetRuntime();
                    builder.TargetFrameworkMoniker(runtime.MsBuildMoniker);

                    return baseJob.WithRuntime(runtime).WithToolchain(builder.ToToolchain());
                case RuntimeMoniker.Wasm:
                    var wasmRuntime = new WasmRuntime(
                        mainJs: options.WasmMainJs,
                        msBuildMoniker: "net5.0",
                        javaScriptEngine: options.WasmJavascriptEngine?.FullName ?? "v8",
                        javaScriptEngineArguments: options.WasmJavaScriptEngineArguments);

                    var toolChain = WasmToolChain.From(new NetCoreAppSettings(
                        targetFrameworkMoniker: wasmRuntime.MsBuildMoniker,
                        runtimeFrameworkVersion: null,
                        name: wasmRuntime.Name,
                        customDotNetCliPath: options.CliPath?.FullName,
                        packagesPath: options.RestorePath?.FullName,
                        timeout: timeOut ?? NetCoreAppSettings.DefaultBuildTimeout,
                        customRuntimePack: options.CustomRuntimePack));

                        return baseJob.WithRuntime(wasmRuntime).WithToolchain(toolChain);
                default:
                    throw new NotSupportedException($"Runtime {runtimeId} is not supported");
            }
        }

        private static IEnumerable<IFilter> GetFilters(CommandLineOptions options)
        {
            if (options.Filters.Any())
                yield return new GlobFilter(options.Filters.ToArray());
            if (options.AllCategories.Any())
                yield return new AllCategoriesFilter(options.AllCategories.ToArray());
            if (options.AnyCategories.Any())
                yield return new AnyCategoriesFilter(options.AnyCategories.ToArray());
            if (options.AttributeNames.Any())
                yield return new AttributesFilter(options.AttributeNames.ToArray());
        }

        private static int GetMaximumDisplayWidth()
        {
            try
            {
                return Console.WindowWidth;
            }
            catch (IOException)
            {
                return MinimumDisplayWidth;
            }
        }

        private static Job CreateCoreRunJob(Job baseJob, CommandLineOptions options, FileInfo coreRunPath)
            => baseJob
                .WithToolchain(new CoreRunToolchain(
                    coreRunPath,
                    createCopy: true,
                    targetFrameworkMoniker: options.Runtimes.SingleOrDefault() ?? RuntimeInformation.GetCurrentRuntime().MsBuildMoniker,
                    customDotNetCliPath: options.CliPath,
                    restorePath: options.RestorePath,
                    displayName: GetCoreRunToolchainDisplayName(options.CoreRunPaths, coreRunPath)));

        private static Job CreateCoreJobWithCli(Job baseJob, CommandLineOptions options)
            => baseJob
                .WithToolchain(CsProjCoreToolchain.From(
                    new NetCoreAppSettings(
                        targetFrameworkMoniker: RuntimeInformation.GetCurrentRuntime().MsBuildMoniker,
                        customDotNetCliPath: options.CliPath?.FullName,
                        runtimeFrameworkVersion: null,
                        name: RuntimeInformation.GetCurrentRuntime().Name,
                        packagesPath: options.RestorePath?.FullName)));

        /// <summary>
        /// we have a limited amount of space when printing the output to the console, so we try to keep things small and simple
        ///
        /// for following paths:
        ///  C:\Projects\coreclr_upstream\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\CoreRun.exe
        ///  C:\Projects\coreclr_upstream\bin\tests\Windows_NT.x64.Release\Tests\Core_Root_beforeMyChanges\CoreRun.exe
        ///
        /// we get:
        ///
        /// \Core_Root\CoreRun.exe
        /// \Core_Root_beforeMyChanges\CoreRun.exe
        /// </summary>
        private static string GetCoreRunToolchainDisplayName(IReadOnlyList<FileInfo> paths, FileInfo coreRunPath)
        {
            if (paths.Count <= 1)
                return "CoreRun";

            int commonLongestPrefixIndex = paths[0].FullName.Length;
            for (int i = 1; i < paths.Count; i++)
            {
                commonLongestPrefixIndex = Math.Min(commonLongestPrefixIndex, paths[i].FullName.Length);
                for (int j = 0; j < commonLongestPrefixIndex; j++)
                    if (paths[i].FullName[j] != paths[0].FullName[j])
                    {
                        commonLongestPrefixIndex = j;
                        break;
                    }
            }


            if (commonLongestPrefixIndex <= 1)
                return coreRunPath.FullName;

            var lastCommonDirectorySeparatorIndex = coreRunPath.FullName.LastIndexOf(Path.DirectorySeparatorChar, commonLongestPrefixIndex - 1);

            return coreRunPath.FullName.Substring(lastCommonDirectorySeparatorIndex);
        }
    }
}
