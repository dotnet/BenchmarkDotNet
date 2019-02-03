using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using CommandLine;

namespace BenchmarkDotNet.ConsoleArguments
{
    public static class ConfigParser
    {
        private const int MinimumDisplayWidth = 80;

        private static readonly IReadOnlyDictionary<string, Job> AvailableJobs = new Dictionary<string, Job>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "default", Job.Default },
            { "dry", Job.Dry },
            { "short", Job.ShortRun },
            { "medium", Job.MediumRun },
            { "long", Job.LongRun },
            { "verylong", Job.VeryLongRun }
        };

        private static readonly ImmutableHashSet<string> AvailableRuntimes = ImmutableHashSet.Create(StringComparer.InvariantCultureIgnoreCase,
            "net461",
            "net462",
            "net47",
            "net471",
            "net472",
            "netcoreapp2.0",
            "netcoreapp2.1",
            "netcoreapp2.2",
            "netcoreapp3.0",
            "clr",
            "core",
            "mono",
            "corert"
        );

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
                if (!AvailableRuntimes.Contains(runtime))
                {
                    logger.WriteLineError($"The provided runtime \"{runtime}\" is invalid. Available options are: {string.Join(", ", AvailableRuntimes.OrderBy(name => name))}.");
                    return false;
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

            return true;
        }

        private static IConfig CreateConfig(CommandLineOptions options, IConfig globalConfig)
        {
            var config = new ManualConfig();

            var baseJob = GetBaseJob(options, globalConfig);
            var expanded = Expand(baseJob.UnfreezeCopy(), options).ToArray(); // UnfreezeCopy ensures that each of the expanded jobs will have it's own ID
            if (expanded.Length > 1)
                expanded[0] = expanded[0].AsBaseline(); // if the user provides multiple jobs, then the first one should be a baseline
            config.Add(expanded); 
            if (config.GetJobs().IsEmpty() && baseJob != Job.Default)
                config.Add(baseJob);

            config.Add(options.Exporters.SelectMany(exporter => AvailableExporters[exporter]).ToArray());
            
            config.Add(options.HardwareCounters
                .Select(counterName => (HardwareCounter)Enum.Parse(typeof(HardwareCounter), counterName, ignoreCase: true))
                .ToArray());

            if (options.UseMemoryDiagnoser)
                config.Add(MemoryDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config.Add(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(recursiveDepth: options.DisassemblerRecursiveDepth, printPrologAndEpilog: true, printDiff: options.DisassemblerDiff)));
            if (!string.IsNullOrEmpty(options.Profiler))
                config.Add(DiagnosersLoader.GetImplementation<IProfiler>(profiler => profiler.ShortName.EqualsWithIgnoreCase(options.Profiler)));

            if (options.DisplayAllStatistics)
                config.Add(StatisticColumn.AllStatistics);
            if (!string.IsNullOrEmpty(options.StatisticalTestThreshold) && Threshold.TryParse(options.StatisticalTestThreshold, out var threshold))
                config.Add(new StatisticalTestColumn(StatisticalTestKind.MannWhitney, threshold));

            if (options.ArtifactsDirectory != null)
                config.ArtifactsPath = options.ArtifactsDirectory.FullName;

            var filters = GetFilters(options).ToArray();
            if (filters.Length > 1)
                config.Add(new UnionFilter(filters));
            else
                config.Add(filters);

            config.Options = config.Options.Set(options.Join, ConfigOptions.JoinSummary);
            config.Options = config.Options.Set(options.KeepBenchmarkFiles, ConfigOptions.KeepBenchmarkFiles);
            config.Options = config.Options.Set(options.StopOnFirstError, ConfigOptions.StopOnFirstError);

            return config;
        }

        private static Job GetBaseJob(CommandLineOptions options, IConfig globalConfig)
        {
            var baseJob = 
                globalConfig?.GetJobs().SingleOrDefault(job => job.Meta.IsDefault) // global config might define single custom Default job
                ?? AvailableJobs[options.BaseJob.ToLowerInvariant()];

            if (baseJob != Job.Dry && options.Outliers != OutlierMode.OnlyUpper)
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
            if (options.RunOncePerIteration)
                baseJob = baseJob.RunOncePerIteration();

            if (AvailableJobs.Values.Contains(baseJob)) // no custom settings
                return baseJob;

            return baseJob
                .AsDefault(false) // after applying all settings from console args the base job is not default anymore
                .AsMutator(); // we mark it as mutator so it will be applied to other jobs defined via attributes and merged later in GetRunnableJobs method
        }

        private static IEnumerable<Job> Expand(Job baseJob, CommandLineOptions options)
        {
            if (options.RunInProcess)
                yield return baseJob.With(InProcessEmitToolchain.Instance);
            else if (!string.IsNullOrEmpty(options.ClrVersion))
                yield return baseJob.With(new ClrRuntime(options.ClrVersion)); // local builds of .NET Runtime
            else if (options.CoreRunPaths.Any())
                foreach (var coreRunPath in options.CoreRunPaths)
                    yield return CreateCoreRunJob(baseJob, options, coreRunPath); // local CoreFX and CoreCLR builds
            else if (options.CliPath != null && options.Runtimes.IsEmpty())
                yield return CreateCoreJobWithCli(baseJob, options);
            else
                foreach (string runtime in options.Runtimes) // known runtimes
                    yield return CreateJobForGivenRuntime(baseJob, runtime.ToLowerInvariant(), options);
        }

        private static Job CreateJobForGivenRuntime(Job baseJob, string runtime, CommandLineOptions options)
        {
            TimeSpan? timeOut = options.TimeOutInSeconds.HasValue ? TimeSpan.FromSeconds(options.TimeOutInSeconds.Value) : default(TimeSpan?);

            switch (runtime)
            {
                case "clr":
                    return baseJob.With(Runtime.Clr);
                case "core":
                    return baseJob.With(Runtime.Core).With(
                        CsProjCoreToolchain.From(
                            NetCoreAppSettings.GetCurrentVersion()
                                .WithCustomDotNetCliPath(options.CliPath?.FullName)
                                .WithCustomPackagesRestorePath(options.RestorePath?.FullName)
                                .WithTimeout(timeOut)));
                case "net461":
                case "net462":
                case "net47":
                case "net471":
                case "net472":
                    return baseJob.With(Runtime.Clr).With(CsProjClassicNetToolchain.From(runtime, options.RestorePath?.FullName, timeOut));
                case "netcoreapp2.0":
                case "netcoreapp2.1":
                case "netcoreapp2.2":
                case "netcoreapp3.0":
                    return baseJob.With(Runtime.Core).With(
                        CsProjCoreToolchain.From(new NetCoreAppSettings(runtime, null, runtime, options.CliPath?.FullName, options.RestorePath?.FullName, timeOut)));
                case "mono":
                    return baseJob.With(new MonoRuntime("Mono", options.MonoPath?.FullName));
                case "corert":
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
                    
                    return baseJob.With(Runtime.CoreRT).With(builder.ToToolchain());
                default:
                    throw new NotSupportedException($"Runtime {runtime} is not supported");
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
                .With(Runtime.Core)
                .With(new CoreRunToolchain(
                    coreRunPath,
                    createCopy: true,
                    targetFrameworkMoniker: options.Runtimes.SingleOrDefault() ?? NetCoreAppSettings.GetCurrentVersion().TargetFrameworkMoniker,
                    customDotNetCliPath: options.CliPath,
                    restorePath: options.RestorePath,
                    displayName: GetCoreRunToolchainDisplayName(options.CoreRunPaths, coreRunPath)));

        private static Job CreateCoreJobWithCli(Job baseJob, CommandLineOptions options)
            => baseJob
                .With(Runtime.Core)
                .With(CsProjCoreToolchain.From(
                    new NetCoreAppSettings(
                        targetFrameworkMoniker: NetCoreAppSettings.GetCurrentVersion().TargetFrameworkMoniker, 
                        customDotNetCliPath: options.CliPath?.FullName,
                        runtimeFrameworkVersion: null,
                        name: NetCoreAppSettings.GetCurrentVersion().TargetFrameworkMoniker,
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