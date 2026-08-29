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
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.Wasm;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.Framework;
using BenchmarkDotNet.Toolchains.R2R;
using CommandLine;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Metrology;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Toolchains.NetCoreApp;

namespace BenchmarkDotNet.ConsoleArguments
{
    public static class ConfigParser
    {
        private const int MinimumDisplayWidth = 80;
        private const char EnvVarKeyValueSeparator = ':';

        private static bool IsUnitlessNumber(string value)
            => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
               && !double.IsNaN(parsed)
               && !double.IsInfinity(parsed);

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
                { "fullxml", new[] { XmlExporter.Full } },
                { "openmetrics", new[] { Exporters.OpenMetrics.OpenMetricsExporter.Default } }
            };

        /// <summary>
        /// Resolves a single <c>--exporters</c> value. A built-in name (see <see cref="AvailableExporters"/>) maps to its
        /// exporter(s); any other value is treated as an assembly-qualified type name of a custom <see cref="IExporter"/>
        /// with a public parameterless constructor. Used by both validation and config creation so the two can't drift.
        /// </summary>
        private static bool TryResolveExporters(string name, [NotNullWhen(true)] out IExporter[]? exporters, [NotNullWhen(false)] out string? error)
        {
            if (AvailableExporters.TryGetValue(name, out var builtIn))
            {
                exporters = builtIn;
                error = null;
                return true;
            }

            exporters = null;

            var type = Type.GetType(name, throwOnError: false, ignoreCase: false);
            if (type == null)
            {
                error = $"The provided exporter \"{name}\" is invalid. Available options are: {string.Join(", ", AvailableExporters.Keys)}. "
                    + "To use a custom exporter, pass its assembly-qualified type name (e.g. \"My.Namespace.MyExporter, MyAssembly\").";
                return false;
            }

            if (!typeof(IExporter).IsAssignableFrom(type))
            {
                error = $"The provided exporter type \"{type.FullName}\" does not implement {nameof(IExporter)}.";
                return false;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                error = $"The provided exporter type \"{type.FullName}\" must have a public parameterless constructor to be used as an exporter.";
                return false;
            }

            exporters = [(IExporter)Activator.CreateInstance(type)!];
            error = null;
            return true;
        }

        public static (bool isSuccess, IConfig? config, CommandLineOptions? options) Parse(string[] args, ILogger logger, IConfig? globalConfig = null)
        {
            (bool isSuccess, IConfig? config, CommandLineOptions? options) result = default;

            var (expandSuccess, expandedArgs) = ExpandResponseFile(args, logger);
            if (!expandSuccess)
            {
                return (false, default, default);
            }

            args = expandedArgs;
            using (var parser = CreateParser(logger))
            {
                parser
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsed(options => result = Validate(options, logger) ? (true, CreateConfig(options, globalConfig, args), options) : (false, default, default))
                    .WithNotParsed(errors => result = (false, default, default));
            }

            return result;
        }

        private static (bool Success, string[] ExpandedTokens) ExpandResponseFile(string[] args, ILogger logger)
        {
            List<string> result = [];
            foreach (var arg in args)
            {
                if (arg.StartsWith("@"))
                {
                    var fileName = arg.Substring(1);
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            var lines = File.ReadAllLines(fileName);
                            foreach (var line in lines)
                            {
                                result.AddRange(ConsumeTokens(line));
                            }
                        }
                        else
                        {
                            logger.WriteLineError($"Response file {fileName} does not exists.");
                            return (false, Array.Empty<string>());
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLineError($"Failed to parse RSP file: {fileName}, {ex.Message}");
                        return (false, Array.Empty<string>());
                    }
                }
                else
                {
                    result.Add(arg);
                }
            }

            return (true, result.ToArray());
        }

        private static IEnumerable<string> ConsumeTokens(string line)
        {
            bool insideQuotes = false;
            var token = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                if (currentChar == ' ' && !insideQuotes)
                {
                    if (token.Length > 0)
                    {
                        yield return GetToken();
                        token = new StringBuilder();
                    }

                    continue;
                }

                if (currentChar == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (currentChar == '\\' && insideQuotes)
                {
                    if (line[i + 1] == '"')
                    {
                        insideQuotes = false;
                        i++;
                        continue;
                    }

                    if (line[i + 1] == '\\')
                    {
                        token.Append('\\');
                        i++;
                        continue;
                    }
                }

                token.Append(currentChar);
            }

            if (token.Length > 0)
            {
                yield return GetToken();
            }

            string GetToken()
            {
                var result = token.ToString();
                if (result.Contains(' '))
                {
                    // Workaround for CommandLine library issue with parsing these kind of args.
                    return " " + result;
                }

                return result;
            }
        }

        internal static bool TryUpdateArgs(string[] args, out string[]? updatedArgs, Action<CommandLineOptions> updater)
        {
            (bool isSuccess, CommandLineOptions? options) result = default;

            ILogger logger = NullLogger.Instance;
            using (var parser = CreateParser(logger))
            {
                parser
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsed(options => result = Validate(options, logger) ? (true, options) : (false, default))
                    .WithNotParsed(errors => result = (false, default));

                if (!result.isSuccess)
                {
                    updatedArgs = null;
                    return false;
                }

                updater(result.options!);

                updatedArgs = parser.FormatCommandLine(result.options, settings => settings.SkipDefault = true).Split();
                return true;
            }
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
            if (options.BaseJob.IsBlank() || !AvailableJobs.ContainsKey(options.BaseJob))
            {
                logger.WriteLineError($"The provided base job \"{options.BaseJob}\" is invalid. Available options are: {string.Join(", ", AvailableJobs.Keys)}.");
                return false;
            }

            foreach (string runtime in options.Runtimes)
            {
                if (!Runtime.TryParse(runtime, out _))
                {
                    logger.WriteLineError($"The provided runtime \"{runtime}\" is invalid. Expected one of:");
                    foreach (string form in KnownRuntimeMonikerForms)
                        logger.WriteLineError($"  {form}");
                    return false;
                }
            }

            foreach (string exporter in options.Exporters)
                if (!TryResolveExporters(exporter, out _, out string? exporterError))
                {
                    logger.WriteLineError(exporterError);
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
                    if (Directory.Exists(coreRunPath.FullName))
                    {
                        logger.WriteLineError($"The provided path to CoreRun: \"{coreRunPath}\" exists but it's a directory, not an executable. You need to include CoreRun.exe (corerun on Unix) in the path.");
                    }
                    else
                    {
                        logger.WriteLineError($"The provided path to CoreRun: \"{coreRunPath}\" does NOT exist.");
                    }

                    return false;
                }

            if (options.MonoPath.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.MonoPath)} \"{options.MonoPath}\" does NOT exist.");
                return false;
            }

            if (options.IlcPackages.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.IlcPackages)} \"{options.IlcPackages}\" does NOT exist.");
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

            if (options.StatisticalTestThreshold.IsNotBlank())
            {
                options.StatisticalTestThreshold = UnitHelper.NormalizeUnits(options.StatisticalTestThreshold.Trim());
                if (IsUnitlessNumber(options.StatisticalTestThreshold))
                {
                    string original = options.StatisticalTestThreshold;
                    options.StatisticalTestThreshold = original + "ns";
                    logger.WriteLineWarning($"No unit suffix supplied for --statisticalTest '{original}'. Interpreting as '{options.StatisticalTestThreshold}' (nanoseconds). If you meant percent, use e.g. '2%'.");
                }

                if (!Threshold.TryParse(options.StatisticalTestThreshold, out _))
                {
                    logger.WriteLineError("Invalid Threshold for Statistical Test. Use --help to see examples.");
                    return false;
                }
            }

            if (options.EnvironmentVariables.Any(envVar => envVar.IndexOf(EnvVarKeyValueSeparator) <= 0))
            {
                logger.WriteLineError($"Environment variable value must be separated from the key using '{EnvVarKeyValueSeparator}'. Use --help to see examples.");
                return false;
            }

            return true;
        }

        private static IConfig CreateConfig(CommandLineOptions options, IConfig? globalConfig, string[] args)
        {
            var config = new ManualConfig();

            var baseJob = GetBaseJob(options, globalConfig);
            var expanded = Expand(baseJob.UnfreezeCopy(), options, args).ToArray(); // UnfreezeCopy ensures that each of the expanded jobs will have it's own ID
            if (expanded.Length > 1)
                expanded[0] = expanded[0].AsBaseline(); // if the user provides multiple jobs, then the first one should be a baseline
            config.AddJob(expanded);
            if (config.GetJobs().IsEmpty() && baseJob != Job.Default)
                config.AddJob(baseJob);

            // Validate() already gated every exporter through TryResolveExporters, so resolution here cannot fail.
            config.AddExporter(options.Exporters.SelectMany(exporter => TryResolveExporters(exporter, out var resolved, out _) ? resolved : []).ToArray());

            config.AddHardwareCounters(options.HardwareCounters
                .Select(counterName => (HardwareCounter)Enum.Parse(typeof(HardwareCounter), counterName, ignoreCase: true))
                .ToArray());

            if (options.UseMemoryDiagnoser)
                config.AddDiagnoser(MemoryDiagnoser.Default);
            if (options.UseThreadingDiagnoser)
                config.AddDiagnoser(ThreadingDiagnoser.Default);
            if (options.UseExceptionDiagnoser)
                config.AddDiagnoser(ExceptionDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
                    maxDepth: options.DisassemblerRecursiveDepth,
                    filters: options.DisassemblerFilters.ToArray(),
                    exportDiff: options.DisassemblerDiff)));
            if (options.Profiler.IsNotBlank())
                config.AddDiagnoser(DiagnosersLoader.GetImplementation<IProfiler>(profiler => profiler.ShortName.EqualsWithIgnoreCase(options.Profiler)));

            if (options.DisplayAllStatistics)
                config.AddColumn(StatisticColumn.AllStatistics);
            if (options.StatisticalTestThreshold.IsNotBlank() && Threshold.TryParse(options.StatisticalTestThreshold, out var threshold))
                config.AddColumn(new StatisticalTestColumn(threshold));

            if (options.ArtifactsDirectory != null)
                config.ArtifactsPath = options.ArtifactsDirectory.FullName;

            if (options.Title.IsNotBlank())
                config.Title = options.Title;

            var filters = GetFilters(options).ToArray();
            if (filters.Length > 1)
                config.AddFilter(new UnionFilter(filters));
            else
                config.AddFilter(filters);

            config.HideColumns(options.HiddenColumns.ToArray());

            config.WithOption(ConfigOptions.JoinSummary, options.Join);
            config.WithOption(ConfigOptions.KeepBenchmarkFiles, options.KeepBenchmarkFiles);
            config.WithOption(ConfigOptions.DontOverwriteResults, options.DontOverwriteResults);
            config.WithOption(ConfigOptions.StopOnFirstError, options.StopOnFirstError);
            config.WithOption(ConfigOptions.DisableLogFile, options.DisableLogFile);
            config.WithOption(ConfigOptions.LogBuildOutput, options.LogBuildOutput);
            config.WithOption(ConfigOptions.GenerateMSBuildBinLog, options.GenerateMSBuildBinLog);
            config.WithOption(ConfigOptions.ApplesToApples, options.ApplesToApples);
            config.WithOption(ConfigOptions.Resume, options.Resume);

            if (config.Options.IsSet(ConfigOptions.GenerateMSBuildBinLog))
                config.Options |= ConfigOptions.KeepBenchmarkFiles;

            if (options.MaxParameterColumnWidth.HasValue)
                config.WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(options.MaxParameterColumnWidth.Value));

            if (options.TimeOutInSeconds.HasValue)
                config.WithBuildTimeout(TimeSpan.FromSeconds(options.TimeOutInSeconds.Value));

            if (options.WakeLock.HasValue)
                config.WithWakeLock(options.WakeLock.Value);

            return config;
        }

        private static Job GetBaseJob(CommandLineOptions options, IConfig? globalConfig)
        {
            var baseJob =
                globalConfig?.GetJobs().SingleOrDefault(job => job.Meta.IsDefault) // global config might define single custom Default job
                ?? AvailableJobs[options.BaseJob.ToLowerInvariant()];

            if (baseJob != Job.Dry && options.Outliers != OutlierMode.RemoveUpper)
                baseJob = baseJob.WithOutlierMode(options.Outliers);

            if (options.Affinity.HasValue)
                baseJob = baseJob.WithAffinity((IntPtr)options.Affinity.Value);

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
            if (options.MemoryRandomization)
                baseJob = baseJob.WithMemoryRandomization();
            if (options.JitTieringMode != Engines.JitTieringMode.Auto)
                baseJob = baseJob.WithJitTieringMode(options.JitTieringMode);
            if (options.NoForcedGCs)
                baseJob = baseJob.WithGcForce(false);
            if (options.EvaluateOverhead is bool evaluateOverhead)
                baseJob = baseJob.WithEvaluateOverhead(evaluateOverhead);
            if (options.ConsumeTasksSynchronously)
                baseJob = baseJob.WithConsumeTasksSynchronously(true);

            if (options.EnvironmentVariables.Any())
            {
                baseJob = baseJob.WithEnvironmentVariables(options.EnvironmentVariables.Select(text =>
                {
                    var separated = text.Split([EnvVarKeyValueSeparator], 2);
                    return new EnvironmentVariable(separated[0], separated[1]);
                }).ToArray());
            }

            if (AvailableJobs.Values.Contains(baseJob)) // no custom settings
                return baseJob;

            return baseJob
                .AsDefault(false) // after applying all settings from console args the base job is not default anymore
                .AsMutator(); // we mark it as mutator so it will be applied to other jobs defined via attributes and merged later in GetRunnableJobs method
        }

        private static IEnumerable<Job> Expand(Job baseJob, CommandLineOptions options, string[] args)
        {
            if (options.RunInProcess)
            {
                yield return Attributes.InProcessAttribute.GetJob(baseJob, Attributes.InProcessToolchainType.Auto, true);
            }
            // --cli and --packages configure a toolchain without selecting one, so with no --runtimes or --corerun to
            // attach them to they are ignored. Creating a job for the host runtime instead would either add a run
            // nobody asked for or override the one the benchmark declares.
            else
            {
                // in case both --runtimes and --corerun are specified, the first one is returned first and becomes a baseline job
                string? first = args.FirstOrDefault(arg =>
                    arg.Equals("--runtimes", StringComparison.OrdinalIgnoreCase)
                    || arg.Equals("-r", StringComparison.OrdinalIgnoreCase)

                    || arg.Equals("--corerun", StringComparison.OrdinalIgnoreCase));

                if (first is null || first.Equals("--corerun", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var coreRunPath in options.CoreRunPaths)
                        yield return CreateCoreRunJob(baseJob, options, coreRunPath); // local dotnet/runtime builds

                    foreach (string runtime in options.Runtimes) // known runtimes
                        yield return CreateJobForGivenRuntime(baseJob, runtime, options);
                }
                else
                {
                    foreach (string runtime in options.Runtimes) // known runtimes
                        yield return CreateJobForGivenRuntime(baseJob, runtime, options);

                    foreach (var coreRunPath in options.CoreRunPaths)
                        yield return CreateCoreRunJob(baseJob, options, coreRunPath); // local dotnet/runtime builds
                }
            }
        }

        private static Job CreateJobForGivenRuntime(Job baseJob, string runtimeId, CommandLineOptions options)
        {
            return Runtime.Parse(runtimeId) switch
            {
                ClrRuntime clr => GetFrameworkJob(clr).WithId(clr.ToString()),
                CoreRuntime core => baseJob.WithId(core.ToString())
                    .WithToolchain(CsProjCoreToolchain.From(core, new(options))),
                NativeAotRuntime aot => baseJob.WithId(aot.ToString())
                    .WithToolchain(CsProjNativeAotToolchain.From(aot, new(options))),
                R2RRuntime r2r => baseJob.WithId(r2r.ToString())
                    .WithToolchain(CsProjR2RToolchain.From(r2r, new(options))),
                MonoWasmRuntime wasm => baseJob.WithId(wasm.ToString())
                    .WithToolchain(CsProjMonoWasmToolchain.From(wasm, new(options))),
                MonoWasmAotRuntime wasmAot => baseJob.WithId(wasmAot.ToString())
                    .WithToolchain(CsProjMonoWasmAotToolchain.From(wasmAot, new(options))),
                CoreWasmRuntime coreWasm => baseJob.WithId(coreWasm.ToString())
                    .WithToolchain(CsProjCoreWasmToolchain.From(coreWasm, new(options))),
                MonoCoreRuntime mono => baseJob.WithId(mono.ToString())
                    .WithToolchain(CsProjMonoCoreToolchain.From(mono, new(options))),
                MonoAotRuntime monoAot => baseJob.WithId(monoAot.ToString())
                    .WithToolchain(RoslynMonoAotToolchain.From(new(options))),
                MonoRuntime mono => baseJob.WithId(mono.ToString())
                    .WithToolchain(RoslynMonoToolchain.From(new(options))),
                _ => throw new NotSupportedException($"Runtime {runtimeId} is not supported"),
            };

            Job GetFrameworkJob(ClrRuntime clr)
            {
                var settings = new FrameworkSettings(options);
                return settings.Equals(FrameworkSettings.Default)
                    // If no custom settings were configured, we just set the runtime so the default toolchain will be auto-selected, which might select the faster Roslyn toolchain.
                    ? baseJob.WithRuntime(clr)
                    : baseJob.WithToolchain(CsProjFrameworkToolchain.From(clr, settings));
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
            if (Console.IsOutputRedirected)
                return MinimumDisplayWidth;

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
                .WithToolchain(CoreRunToolchain.From(new CoreRunSettings(options)
                {
                    SourceCoreRun = coreRunPath,
                    TargetFrameworkMoniker = RuntimeInformation.GetCurrentRuntime() is CoreRuntime core
                        ? core.GetTfm() // netcoreappX.Y for < 5, netX.0 for 5+
                        : CoreRuntime.Latest.GetTfm(), // non-Core host; use most recent tfm, as the toolchain is being used only by dotnet/runtime contributors
                    DisplayName = GetCoreRunToolchainDisplayName(options.CoreRunPaths, coreRunPath),
                }));

        /// <summary>
        /// The moniker forms <see cref="Runtime.TryParse" /> accepts, for the "invalid runtime" error. Listed in the
        /// same order as its prefix dispatch, so the two can be compared at a glance.
        /// </summary>
        private static readonly string[] KnownRuntimeMonikerForms =
        [
            "net<version>              e.g. net472, net8.0, net8.0-windows (.NET 5.0+ only)",
            "netcoreapp<version>       e.g. netcoreapp3.1",
            "nativeaot<version>        e.g. nativeaot8.0",
            "r2r<version>              e.g. r2r8.0",
            "mono                      classic Mono",
            "mono<version>             .NET on the Mono VM, e.g. mono8.0",
            "monoaot                   legacy Mono AOT, versionless",
            "monowasm<version>         e.g. monowasm8.0",
            "monowasmaot<version>      e.g. monowasmaot8.0",
            "corewasm<version>         e.g. corewasm11.0",
        ];

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
