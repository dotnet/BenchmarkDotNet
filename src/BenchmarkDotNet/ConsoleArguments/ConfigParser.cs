using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.OpenMetrics;
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
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Metrology;
using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Toolchains.NetCoreApp;

namespace BenchmarkDotNet.ConsoleArguments
{
    public static class ConfigParser
    {
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

        private static readonly IReadOnlyDictionary<string, IExporter[]> AvailableExporters =
            new Dictionary<string, IExporter[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["csv"] = [CsvExporter.Default],
                ["csvmeasurements"] = [CsvMeasurementsExporter.Default],
                ["html"] = [HtmlExporter.Default],
                ["markdown"] = [MarkdownExporter.Default],
                ["atlassian"] = [MarkdownExporter.Atlassian],
                ["stackoverflow"] = [MarkdownExporter.StackOverflow],
                ["github"] = [MarkdownExporter.GitHub],
                ["plain"] = [PlainExporter.Default],
                ["rplot"] = [CsvMeasurementsExporter.Default, RPlotExporter.Default], // R Plots depends on having the full measurements available
                ["json"] = [JsonExporter.Default],
                ["briefjson"] = [JsonExporter.Brief],
                ["fulljson"] = [JsonExporter.Full],
                ["asciidoc"] = [AsciiDocExporter.Default],
                ["xml"] = [XmlExporter.Default],
                ["briefxml"] = [XmlExporter.Brief],
                ["fullxml"] = [XmlExporter.Full],
                ["openmetrics"] = [OpenMetricsExporter.Default],
            };

        internal static RootCommand RootCommand
        {
            get
            {
                using var invariantUICultureScope = Helpers.CultureInfoHelper.CreateInvariantUICultureScope();

                var rootCommand = new RootCommand("BenchmarkDotNet Command Line options")
                {
                    CommandLineOptions.BaseJobOption,
                    CommandLineOptions.RuntimesOption,
                    CommandLineOptions.ExportersOption,
                    CommandLineOptions.MemoryOption,
                    CommandLineOptions.ThreadingOption,
                    CommandLineOptions.ExceptionsOption,
                    CommandLineOptions.DisassemblyOption,
                    CommandLineOptions.ProfilerOption,
                    CommandLineOptions.FiltersOption,
                    CommandLineOptions.HiddenColumnsOption,
                    CommandLineOptions.RunInProcessOption,
                    CommandLineOptions.ArtifactsDirectoryOption,
                    CommandLineOptions.OutliersOption,
                    CommandLineOptions.AffinityOption,
                    CommandLineOptions.DisplayAllStatisticsOption,
                    CommandLineOptions.AllCategoriesOption,
                    CommandLineOptions.AnyCategoriesOption,
                    CommandLineOptions.AttributeNamesOption,
                    CommandLineOptions.JoinOption,
                    CommandLineOptions.KeepBenchmarkFilesOption,
                    CommandLineOptions.DontOverwriteResultsOption,
                    CommandLineOptions.HardwareCountersOption,
                    CommandLineOptions.CliPathOption,
                    CommandLineOptions.RestorePathOption,
                    CommandLineOptions.CoreRunPathsOption,
                    CommandLineOptions.MonoPathOption,
                    CommandLineOptions.ClrVersionOption,
                    CommandLineOptions.ILCompilerVersionOption,
                    CommandLineOptions.IlcPackagesOption,
                    CommandLineOptions.LaunchCountOption,
                    CommandLineOptions.WarmupCountOption,
                    CommandLineOptions.MinWarmupCountOption,
                    CommandLineOptions.MaxWarmupCountOption,
                    CommandLineOptions.IterationTimeOption,
                    CommandLineOptions.IterationCountOption,
                    CommandLineOptions.MinIterationCountOption,
                    CommandLineOptions.MaxIterationCountOption,
                    CommandLineOptions.InvocationCountOption,
                    CommandLineOptions.UnrollFactorOption,
                    CommandLineOptions.RunStrategyOption,
                    CommandLineOptions.PlatformOption,
                    CommandLineOptions.RunOnceOption,
                    CommandLineOptions.PrintInformationOption,
                    CommandLineOptions.ApplesToApplesOption,
                    CommandLineOptions.ListBenchmarkCaseModeOption,
                    CommandLineOptions.DisassemblerDepthOption,
                    CommandLineOptions.DisassemblerFiltersOption,
                    CommandLineOptions.DisassemblerDiffOption,
                    CommandLineOptions.LogBuildOutputOption,
                    CommandLineOptions.GenerateBinLogOption,
                    CommandLineOptions.TimeoutOption,
                    CommandLineOptions.WakeLockOption,
                    CommandLineOptions.StopOnFirstErrorOption,
                    CommandLineOptions.StatisticalTestThresholdOption,
                    CommandLineOptions.DisableLogFileOption,
                    CommandLineOptions.MaxParameterColumnWidthOption,
                    CommandLineOptions.EnvironmentVariablesOption,
                    CommandLineOptions.MemoryRandomizationOption,
                    CommandLineOptions.JitTieringModeOption,
                    CommandLineOptions.WasmJavascriptEngineOption,
                    CommandLineOptions.WasmJavaScriptEngineArgumentsOption,
                    CommandLineOptions.WasmMainJsTemplateOption,
                    CommandLineOptions.CustomRuntimePackOption,
                    CommandLineOptions.AOTCompilerPathOption,
                    CommandLineOptions.AOTCompilerModeOption,
                    CommandLineOptions.WasmRuntimeFlavorOption,
                    CommandLineOptions.WasmProcessTimeoutMinutesOption,
                    CommandLineOptions.NoForcedGCsOption,
                    CommandLineOptions.EvaluateOverheadOption,
                    CommandLineOptions.ResumeOption,
                };

                return rootCommand;
            }
        }

        private static readonly Dictionary<string, string> AliasToCanonical
            = new(StringComparer.OrdinalIgnoreCase)
            {
                // Aliases
                ["-j"] = "--job",
                ["-r"] = "--runtimes",
                ["-e"] = "--exporters",
                ["-m"] = "--memory",
                ["-t"] = "--threading",
                ["-d"] = "--disasm",
                ["-p"] = "--profiler",
                ["-f"] = "--filter",
                ["-h"] = "--hide",
                ["-i"] = "--inProcess",
                ["-a"] = "--artifacts",

                // Names
                ["--job"] = "--job",
                ["--runtimes"] = "--runtimes",
                ["--exporters"] = "--exporters",
                ["--memory"] = "--memory",
                ["--threading"] = "--threading",
                ["--exceptions"] = "--exceptions",
                ["--disasm"] = "--disasm",
                ["--profiler"] = "--profiler",
                ["--filter"] = "--filter",
                ["--hide"] = "--hide",
                ["--inprocess"] = "--inProcess",
                ["--artifacts"] = "--artifacts",
                ["--outliers"] = "--outliers",
                ["--affinity"] = "--affinity",
                ["--allstats"] = "--allStats",
                ["--allcategories"] = "--allCategories",
                ["--anycategories"] = "--anyCategories",
                ["--attribute"] = "--attribute",
                ["--join"] = "--join",
                ["--keepfiles"] = "--keepFiles",
                ["--nooverwrite"] = "--noOverwrite",
                ["--counters"] = "--counters",
                ["--cli"] = "--cli",
                ["--packages"] = "--packages",
                ["--corerun"] = "--coreRun",
                ["--monopath"] = "--monoPath",
                ["--clrversion"] = "--clrVersion",
                ["--ilcompilerversion"] = "--ilCompilerVersion",
                ["--ilcpackages"] = "--ilcPackages",
                ["--launchcount"] = "--launchCount",
                ["--warmupcount"] = "--warmupCount",
                ["--minwarmupcount"] = "--minWarmupCount",
                ["--maxwarmupcount"] = "--maxWarmupCount",
                ["--iterationtime"] = "--iterationTime",
                ["--iterationcount"] = "--iterationCount",
                ["--miniterationcount"] = "--minIterationCount",
                ["--maxiterationcount"] = "--maxIterationCount",
                ["--invocationcount"] = "--invocationCount",
                ["--unrollfactor"] = "--unrollFactor",
                ["--strategy"] = "--strategy",
                ["--platform"] = "--platform",
                ["--runonceperiteration"] = "--runOncePerIteration",
                ["--info"] = "--info",
                ["--apples"] = "--apples",
                ["--list"] = "--list",
                ["--disasmdepth"] = "--disasmDepth",
                ["--disasmfilter"] = "--disasmFilter",
                ["--disasmdiff"] = "--disasmDiff",
                ["--logbuildoutput"] = "--logBuildOutput",
                ["--generatebinlog"] = "--generateBinLog",
                ["--buildtimeout"] = "--buildTimeout",
                ["--wakelock"] = "--wakeLock",
                ["--stoponfirsterror"] = "--stopOnFirstError",
                ["--statisticaltest"] = "--statisticalTest",
                ["--disablelogfile"] = "--disableLogFile",
                ["--maxwidth"] = "--maxWidth",
                ["--envvars"] = "--envVars",
                ["--memoryrandomization"] = "--memoryRandomization",
                ["--jittieringmode"] = "--jitTieringMode",
                ["--wasmengine"] = "--wasmEngine",
                ["--wasmargs"] = "--wasmArgs",
                ["--wasmmainjstemplate"] = "--wasmMainJsTemplate",
                ["--customruntimepack"] = "--customRuntimePack",
                ["--aotcompilerpath"] = "--AOTCompilerPath",
                ["--aotcompilermode"] = "--AOTCompilerMode",
                ["--wasmruntimeflavor"] = "--wasmRuntimeFlavor",
                ["--wasmprocesstimeout"] = "--wasmProcessTimeout",
                ["--noforcedgcs"] = "--noForcedGCs",
                ["--evaluateOverhead"] = "--evaluateOverhead",
                ["--resume"] = "--resume",
            };

        private static bool HasDuplicateOptions(string[] args)
        {
            // Gets canonical option names.
            var options = args.Where(x => x.StartsWith("-") && x != "--")
                              .Select(x => x.Split('=')[0].ToLowerInvariant())
                              .Select(x => AliasToCanonical.TryGetValue(x, out var c) ? c : x);

            // Return true if any canonical option name appears more than once.
            return options.GroupBy(x => x).Any(g => g.Count() > 1);
        }

        private static string[] NormalizeArgs(string[] args)
        {
            var result = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // Handle extra args that are appended after `--`
                if (arg == "--")
                {
                    result.AddRange(args.Skip(i));
                    break;
                }

                if (!arg.StartsWith("-"))
                {
                    result.Add(arg);
                    continue;
                }

                var values = arg.Split(['='], 2);

                var key = values[0];
                var value = values.Length > 1 ? values[1] : null;

                if (AliasToCanonical.TryGetValue(key, out var canonical))
                    key = canonical;

                if (key == "--counters")
                {
                    result.Add(key);
                    if (value != null)
                    {
                        result.AddRange(value.Split('+'));
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && args[i + 1].Contains('+'))
                    {
                        i++;
                        result.AddRange(args[i].Split('+'));
                    }
                    continue;
                }

                arg = value != null ? $"{key}={value}" : key;

                result.Add(arg);
            }

            return result.ToArray();
        }

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
            args = args.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();

            if (HasDuplicateOptions(args))
            {
                logger.WriteLineError("Duplicate options are not allowed.");
                return (false, default, default);
            }

            var (expandSuccess, expandedArgs) = ExpandResponseFile(args, logger);
            if (!expandSuccess) return (false, default, default);
            args = expandedArgs;
            args = NormalizeArgs(args);

            // Handle extra arguments that are passed after " -- ". These
            string[] extraArgs = [];
            var dashDashIndex = Array.IndexOf(args, "--");
            if (dashDashIndex >= 0)
            {
                extraArgs = args.Skip(dashDashIndex + 1).ToArray();
                args = args.Take(dashDashIndex).ToArray();
            }

            var parseResult = RootCommand.Parse(args);

            if (args.Any(a => a == "-h" || a == "--help" || a == "-?" || a == "--version"))
            {
                using var invariantUICultureScope = Helpers.CultureInfoHelper.CreateInvariantUICultureScope();
                using var writer = new StringWriter();
                parseResult.Invoke(new InvocationConfiguration { Output = writer });
                logger.Write(writer.ToString());
                return (true, default, default);
            }

            if (parseResult.Errors.Any())
            {
                foreach (var error in parseResult.Errors)
                {
                    string msg = error.Message;

                    var badArg = args.FirstOrDefault(a => a.StartsWith("-") && msg.Contains(a));

                    if (badArg != null)
                    {
                        msg = $"Option '{badArg.TrimStart('-')}' is unknown.";
                    }

                    logger.WriteLineError(msg);
                }
                return (false, default, default);
            }

            var invalidOptions = parseResult.UnmatchedTokens.Where(t => t.StartsWith("-")).ToList();
            if (invalidOptions.Any())
            {
                foreach (var opt in invalidOptions)
                    logger.WriteLineError($"Option '{opt.TrimStart('-')}' is unknown.");
                return (false, default, default);
            }

            var options = ToCommandLineOptions(parseResult, extraArgs);

            bool isSuccess = Validate(options, logger);
            return isSuccess
                ? (true, CreateConfig(options, globalConfig, args), options)
                : (false, default, default);
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
                return result;
            }
        }

        internal static bool TryUpdateArgs(string[] args, out string[]? updatedArgs, Action<CommandLineOptions> updater)
        {
            args = args.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();

            if (HasDuplicateOptions(args))
            {
                updatedArgs = null;
                return false;
            }

            args = NormalizeArgs(args);

            string[] extraArgs = [];
            var dashDashIndex = Array.IndexOf(args, "--");
            if (dashDashIndex >= 0)
            {
                extraArgs = args.Skip(dashDashIndex + 1).ToArray();
                args = args.Take(dashDashIndex).ToArray();
            }

            var parseResult = RootCommand.Parse(args);

            var invalidOptions = parseResult.UnmatchedTokens.Where(t => t.StartsWith("-")).ToList();
            CommandLineOptions options = ToCommandLineOptions(parseResult, extraArgs);

            if (invalidOptions.Any() || !Validate(options, NullLogger.Instance))
            {
                updatedArgs = null;
                return false;
            }

            updater(options);

            updatedArgs = SerializeToArgs(options);
            return true;
        }

        private static CommandLineOptions ToCommandLineOptions(ParseResult parseResult, string[] extraArgs)
            => new CommandLineOptions
            {
                ExtraArguments = extraArgs,
                BaseJob = parseResult.GetValue(CommandLineOptions.BaseJobOption) ?? "",
                Runtimes = parseResult.GetValue(CommandLineOptions.RuntimesOption) ?? [],
                Exporters = parseResult.GetValue(CommandLineOptions.ExportersOption) ?? [],
                UseMemoryDiagnoser = parseResult.GetValue(CommandLineOptions.MemoryOption),
                UseThreadingDiagnoser = parseResult.GetValue(CommandLineOptions.ThreadingOption),
                UseExceptionDiagnoser = parseResult.GetValue(CommandLineOptions.ExceptionsOption),
                UseDisassemblyDiagnoser = parseResult.GetValue(CommandLineOptions.DisassemblyOption),
                Profiler = parseResult.GetValue(CommandLineOptions.ProfilerOption) ?? "",
                Filters = (parseResult.GetValue(CommandLineOptions.FiltersOption) ?? [])
                            .Concat(parseResult.UnmatchedTokens.Where(t => !t.StartsWith("-")))
                            .ToArray(),
                HiddenColumns = parseResult.GetValue(CommandLineOptions.HiddenColumnsOption) ?? [],
                RunInProcess = parseResult.GetValue(CommandLineOptions.RunInProcessOption),
                ArtifactsDirectory = parseResult.GetValue(CommandLineOptions.ArtifactsDirectoryOption),
                Outliers = parseResult.GetValue(CommandLineOptions.OutliersOption),
                Affinity = parseResult.GetValue(CommandLineOptions.AffinityOption),
                DisplayAllStatistics = parseResult.GetValue(CommandLineOptions.DisplayAllStatisticsOption),
                AllCategories = parseResult.GetValue(CommandLineOptions.AllCategoriesOption) ?? [],
                AnyCategories = parseResult.GetValue(CommandLineOptions.AnyCategoriesOption) ?? [],
                AttributeNames = parseResult.GetValue(CommandLineOptions.AttributeNamesOption) ?? [],
                Join = parseResult.GetValue(CommandLineOptions.JoinOption),
                KeepBenchmarkFiles = parseResult.GetValue(CommandLineOptions.KeepBenchmarkFilesOption),
                DontOverwriteResults = parseResult.GetValue(CommandLineOptions.DontOverwriteResultsOption),
                HardwareCounters = parseResult.GetValue(CommandLineOptions.HardwareCountersOption) ?? [],
                CliPath = parseResult.GetValue(CommandLineOptions.CliPathOption),
                RestorePath = parseResult.GetValue(CommandLineOptions.RestorePathOption) != null
                    ? new DirectoryInfo(parseResult.GetValue(CommandLineOptions.RestorePathOption)!.FullName)
                    : null,
                CoreRunPaths = parseResult.GetValue(CommandLineOptions.CoreRunPathsOption) ?? [],
                MonoPath = parseResult.GetValue(CommandLineOptions.MonoPathOption),
                ClrVersion = parseResult.GetValue(CommandLineOptions.ClrVersionOption) ?? "",
                ILCompilerVersion = parseResult.GetValue(CommandLineOptions.ILCompilerVersionOption) ?? "",
                IlcPackages = parseResult.GetValue(CommandLineOptions.IlcPackagesOption),
                LaunchCount = parseResult.GetValue(CommandLineOptions.LaunchCountOption),
                WarmupIterationCount = parseResult.GetValue(CommandLineOptions.WarmupCountOption),
                MinWarmupIterationCount = parseResult.GetValue(CommandLineOptions.MinWarmupCountOption),
                MaxWarmupIterationCount = parseResult.GetValue(CommandLineOptions.MaxWarmupCountOption),
                IterationTimeInMilliseconds = parseResult.GetValue(CommandLineOptions.IterationTimeOption),
                IterationCount = parseResult.GetValue(CommandLineOptions.IterationCountOption),
                MinIterationCount = parseResult.GetValue(CommandLineOptions.MinIterationCountOption),
                MaxIterationCount = parseResult.GetValue(CommandLineOptions.MaxIterationCountOption),
                InvocationCount = parseResult.GetValue(CommandLineOptions.InvocationCountOption),
                UnrollFactor = parseResult.GetValue(CommandLineOptions.UnrollFactorOption),
                RunStrategy = parseResult.GetValue(CommandLineOptions.RunStrategyOption),
                Platform = parseResult.GetValue(CommandLineOptions.PlatformOption),
                RunOncePerIteration = parseResult.GetValue(CommandLineOptions.RunOnceOption),
                PrintInformation = parseResult.GetValue(CommandLineOptions.PrintInformationOption),
                ApplesToApples = parseResult.GetValue(CommandLineOptions.ApplesToApplesOption),
                ListBenchmarkCaseMode = parseResult.GetValue(CommandLineOptions.ListBenchmarkCaseModeOption),
                DisassemblerRecursiveDepth = parseResult.GetValue(CommandLineOptions.DisassemblerDepthOption),
                DisassemblerFilters = parseResult.GetValue(CommandLineOptions.DisassemblerFiltersOption) ?? [],
                DisassemblerDiff = parseResult.GetValue(CommandLineOptions.DisassemblerDiffOption),
                LogBuildOutput = parseResult.GetValue(CommandLineOptions.LogBuildOutputOption),
                GenerateMSBuildBinLog = parseResult.GetValue(CommandLineOptions.GenerateBinLogOption),
                TimeOutInSeconds = parseResult.GetValue(CommandLineOptions.TimeoutOption),
                WakeLock = parseResult.GetValue(CommandLineOptions.WakeLockOption),
                StopOnFirstError = parseResult.GetValue(CommandLineOptions.StopOnFirstErrorOption),
                StatisticalTestThreshold = parseResult.GetValue(CommandLineOptions.StatisticalTestThresholdOption) ?? "",
                DisableLogFile = parseResult.GetValue(CommandLineOptions.DisableLogFileOption),
                MaxParameterColumnWidth = parseResult.GetValue(CommandLineOptions.MaxParameterColumnWidthOption),
                EnvironmentVariables = parseResult.GetValue(CommandLineOptions.EnvironmentVariablesOption) ?? [],
                MemoryRandomization = parseResult.GetValue(CommandLineOptions.MemoryRandomizationOption),
                JitTieringMode = parseResult.GetValue(CommandLineOptions.JitTieringModeOption),
                WasmJavaScriptEngine = parseResult.GetValue(CommandLineOptions.WasmJavascriptEngineOption) ?? "",
                WasmJavaScriptEngineArguments = parseResult.GetValue(CommandLineOptions.WasmJavaScriptEngineArgumentsOption) ?? "",
                WasmMainJsTemplate = parseResult.GetValue(CommandLineOptions.WasmMainJsTemplateOption),
                CustomRuntimePack = parseResult.GetValue(CommandLineOptions.CustomRuntimePackOption) ?? "",
                AOTCompilerPath = parseResult.GetValue(CommandLineOptions.AOTCompilerPathOption),
                AOTCompilerMode = parseResult.GetValue(CommandLineOptions.AOTCompilerModeOption),
                WasmRuntimeFlavor = parseResult.GetValue(CommandLineOptions.WasmRuntimeFlavorOption),
                WasmProcessTimeoutMinutes = parseResult.GetValue(CommandLineOptions.WasmProcessTimeoutMinutesOption),
                NoForcedGCs = parseResult.GetValue(CommandLineOptions.NoForcedGCsOption),
                EvaluateOverhead = parseResult.GetValue(CommandLineOptions.EvaluateOverheadOption),
                Resume = parseResult.GetValue(CommandLineOptions.ResumeOption),
            };

        /// <summary>
        /// Serialize parsed options to string array.
        /// Option's default value should be omitted.
        /// </summary>
        internal static string[] SerializeToArgs(CommandLineOptions options)
        {
            var result = new List<string>();

            if (options.Filters.Any())
            {
                result.Add("--filter");
                result.AddRange(options.Filters);
            }

            if (options.BaseJob.IsNotBlank() && !options.BaseJob.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("--job");
                result.Add(options.BaseJob);
            }

            // Boolean options
            if (options.UseMemoryDiagnoser) result.Add("--memory");
            if (options.UseThreadingDiagnoser) result.Add("--threading");
            if (options.UseExceptionDiagnoser) result.Add("--exceptions");
            if (options.UseDisassemblyDiagnoser) result.Add("--disasm");
            if (options.RunInProcess) result.Add("--inProcess");
            if (options.Join) result.Add("--join");
            if (options.KeepBenchmarkFiles) result.Add("--keepFiles");
            if (options.DontOverwriteResults) result.Add("--noOverwrite");
            if (options.DisplayAllStatistics) result.Add("--allStats");
            if (options.RunOncePerIteration) result.Add("--runOncePerIteration");
            if (options.PrintInformation) result.Add("--info");
            if (options.ApplesToApples) result.Add("--apples");
            if (options.LogBuildOutput) result.Add("--logBuildOutput");
            if (options.GenerateMSBuildBinLog) result.Add("--generateBinLog");
            if (options.StopOnFirstError) result.Add("--stopOnFirstError");
            if (options.DisableLogFile) result.Add("--disableLogFile");
            if (options.MemoryRandomization) result.Add("--memoryRandomization");
            if (options.DisassemblerDiff) result.Add("--disasmDiff");
            if (options.NoForcedGCs) result.Add("--noForcedGCs");
            if (options.EvaluateOverhead) result.Add("--evaluateOverhead");
            if (options.Resume) result.Add("--resume");

            // Multi values options
            if (options.Runtimes.Any())
            {
                result.Add("--runtimes");
                result.AddRange(options.Runtimes);
            }

            if (options.Profiler.IsNotBlank())
                result.AddRange(["--profiler", options.Profiler]);

            if (options.ClrVersion.IsNotBlank())
                result.AddRange(["--clrVersion", options.ClrVersion]);

            if (options.StatisticalTestThreshold.IsNotBlank())
                result.AddRange(["--statisticalTest", options.StatisticalTestThreshold]);

            if (options.ILCompilerVersion.IsNotBlank())
                result.AddRange(["--ilCompilerVersion", options.ILCompilerVersion]);

            if (options.CustomRuntimePack.IsNotBlank())
                result.AddRange(["--customRuntimePack", options.CustomRuntimePack]);

            // Add multiple values
            if (options.Exporters.Any())
            {
                result.Add("--exporters");
                result.AddRange(options.Exporters);
            }

            if (options.HardwareCounters.Any())
            {
                result.Add("--counters");
                result.AddRange(options.HardwareCounters);
            }

            if (options.AllCategories.Any())
            {
                result.Add("--allCategories");
                result.AddRange(options.AllCategories);
            }

            if (options.AnyCategories.Any())
            {
                result.Add("--anyCategories");
                result.AddRange(options.AnyCategories);
            }

            if (options.AttributeNames.Any())
            {
                result.Add("--attribute");
                result.AddRange(options.AttributeNames);
            }

            if (options.HiddenColumns.Any())
            {
                result.Add("--hide");
                result.AddRange(options.HiddenColumns);
            }

            if (options.DisassemblerFilters.Any())
            {
                result.Add("--disasmFilter");
                result.AddRange(options.DisassemblerFilters);
            }

            if (options.EnvironmentVariables.Any())
            {
                result.Add("--envVars");
                result.AddRange(options.EnvironmentVariables);
            }

            // Add nullable value
            if (options.LaunchCount.HasValue)
            {
                result.Add("--launchCount");
                result.Add(options.LaunchCount.Value.ToString());
            }

            if (options.WarmupIterationCount.HasValue)
            { result.Add("--warmupCount"); result.Add(options.WarmupIterationCount.Value.ToString()); }

            if (options.MinWarmupIterationCount.HasValue)
            { result.Add("--minWarmupCount"); result.Add(options.MinWarmupIterationCount.Value.ToString()); }

            if (options.MaxWarmupIterationCount.HasValue)
            { result.Add("--maxWarmupCount"); result.Add(options.MaxWarmupIterationCount.Value.ToString()); }

            if (options.IterationTimeInMilliseconds.HasValue)
            { result.Add("--iterationTime"); result.Add(options.IterationTimeInMilliseconds.Value.ToString()); }

            if (options.IterationCount.HasValue)
            { result.Add("--iterationCount"); result.Add(options.IterationCount.Value.ToString()); }

            if (options.MinIterationCount.HasValue)
            { result.Add("--minIterationCount"); result.Add(options.MinIterationCount.Value.ToString()); }

            if (options.MaxIterationCount.HasValue)
            { result.Add("--maxIterationCount"); result.Add(options.MaxIterationCount.Value.ToString()); }

            if (options.InvocationCount.HasValue)
            { result.Add("--invocationCount"); result.Add(options.InvocationCount.Value.ToString()); }

            if (options.UnrollFactor.HasValue)
            { result.Add("--unrollFactor"); result.Add(options.UnrollFactor.Value.ToString()); }

            if (options.Affinity.HasValue)
            { result.Add("--affinity"); result.Add(options.Affinity.Value.ToString()); }

            if (options.TimeOutInSeconds.HasValue)
            { result.Add("--buildTimeout"); result.Add(options.TimeOutInSeconds.Value.ToString()); }

            if (options.MaxParameterColumnWidth.HasValue)
            { result.Add("--maxWidth"); result.Add(options.MaxParameterColumnWidth.Value.ToString()); }

            if (options.DisassemblerRecursiveDepth != 1)
            { result.Add("--disasmDepth"); result.Add(options.DisassemblerRecursiveDepth.ToString()); }

            if (options.Outliers != OutlierMode.RemoveUpper)
            { result.Add("--outliers"); result.Add(options.Outliers.ToString()); }

            if (options.RunStrategy.HasValue)
            { result.Add("--strategy"); result.Add(options.RunStrategy.Value.ToString()); }

            if (options.Platform.HasValue)
            { result.Add("--platform"); result.Add(options.Platform.Value.ToString()); }

            if (options.WakeLock.HasValue)
            { result.Add("--wakeLock"); result.Add(options.WakeLock.Value.ToString()); }

            if (options.ListBenchmarkCaseMode != ListBenchmarkCaseMode.Disabled)
            { result.Add("--list"); result.Add(options.ListBenchmarkCaseMode.ToString()); }

            if (options.AOTCompilerMode != MonoAotCompilerMode.mini)
            { result.Add("--AOTCompilerMode"); result.Add(options.AOTCompilerMode.ToString()); }

            if (options.ArtifactsDirectory != null)
            { result.Add("--artifacts"); result.Add(options.ArtifactsDirectory.FullName); }

            if (options.CliPath != null)
            { result.Add("--cli"); result.Add(options.CliPath.FullName); }

            if (options.RestorePath != null)
            { result.Add("--packages"); result.Add(options.RestorePath.FullName); }

            if (options.MonoPath != null)
            { result.Add("--monoPath"); result.Add(options.MonoPath.FullName); }

            if (options.IlcPackages != null)
            { result.Add("--ilcPackages"); result.Add(options.IlcPackages.FullName); }

            if (options.JitTieringMode != Engines.JitTieringMode.Auto)
            { result.Add("--jitTieringMode"); result.Add(options.JitTieringMode.ToString()); }

            if (options.WasmJavaScriptEngine.IsNotBlank() && options.WasmJavaScriptEngine != "v8")
            { result.Add("--wasmEngine"); result.Add(options.WasmJavaScriptEngine); }

            if (options.WasmJavaScriptEngineArguments.IsNotBlank() && options.WasmJavaScriptEngineArguments != "--expose_wasm")
            { result.Add("--wasmArgs"); result.Add(options.WasmJavaScriptEngineArguments); }

            if (options.AOTCompilerPath != null)
            { result.Add("--AOTCompilerPath"); result.Add(options.AOTCompilerPath.FullName); }

            if (options.WasmRuntimeFlavor != RuntimeFlavor.Mono)
            { result.Add("--wasmRuntimeFlavor"); result.Add(options.WasmRuntimeFlavor.ToString()); }

            if (options.WasmProcessTimeoutMinutes != 10)
            { result.Add("--wasmProcessTimeout"); result.Add(options.WasmProcessTimeoutMinutes.ToString()); }

            if (options.CoreRunPaths.Any())
            { result.Add("--coreRun"); result.AddRange(options.CoreRunPaths.Select(p => p.FullName)); }

            return result.ToArray();
        }

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

            if (options.Affinity.HasValue && !TryConvertAffinity(options.Affinity.Value, IntPtr.Size, out _))
            {
                logger.WriteLineError($"The provided affinity mask 0x{options.Affinity.Value:X} does not fit into the {IntPtr.Size * 8} bit process that hosts the benchmarks. Use a mask of at most 32 bits or run in a 64 bit process.");
                return false;
            }

            return true;
        }

        // a 32 bit process only reaches the first 32 processors and new IntPtr(long) throws there instead of truncating
        internal static bool TryConvertAffinity(ulong mask, int pointerSize, out IntPtr affinity)
        {
            if (pointerSize >= 8)
            {
                affinity = new IntPtr(unchecked((long)mask));
                return true;
            }

            affinity = new IntPtr(unchecked((int)mask));
            return mask <= uint.MaxValue;
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

            if (options.Affinity.HasValue && TryConvertAffinity(options.Affinity.Value, IntPtr.Size, out var affinity))
                baseJob = baseJob.WithAffinity(affinity);

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
            if (options.EvaluateOverhead)
                baseJob = baseJob.WithEvaluateOverhead(true);

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

        private static Job CreateAotJob(Job baseJob, CommandLineOptions options, RuntimeMoniker runtimeMoniker, string ilCompilerVersion, string nuGetFeedUrl = "")
        {
            var builder = NativeAotToolchain.CreateBuilder();

            if (options.CliPath != null)
                builder.DotNetCli(options.CliPath.FullName);
            if (options.RestorePath != null)
                builder.PackagesRestorePath(options.RestorePath.FullName);

            if (options.IlcPackages != null)
                builder.UseLocalBuild(options.IlcPackages);
            else if (options.ILCompilerVersion.IsNotBlank())
                builder.UseNuGet(options.ILCompilerVersion, nuGetFeedUrl);
            else
                builder.UseNuGet(ilCompilerVersion, nuGetFeedUrl);

            var runtime = runtimeMoniker.GetRuntime();
            builder.TargetFrameworkMoniker(runtime.MsBuildMoniker);

            return baseJob.WithRuntime(runtime).WithToolchain(builder.ToToolchain()).WithId(runtime.Name);
        }

        private static Job MakeMonoJob(Job baseJob, CommandLineOptions options, MonoRuntime runtime)
        {
            return baseJob
                .WithRuntime(runtime)
                .WithToolchain(MonoToolchain.From(
                    new NetCoreAppSettings(
                        targetFrameworkMoniker: runtime.MsBuildMoniker,
                        runtimeFrameworkVersion: "",
                        name: runtime.Name,
                        options: options)));
        }

        private static Job MakeMonoAOTLLVMJob(Job baseJob, CommandLineOptions options, string msBuildMoniker, RuntimeMoniker moniker)
        {
            var monoAotLLVMRuntime = new MonoAotLLVMRuntime(
                aotCompilerPath: options.AOTCompilerPath,
                aotCompilerMode: options.AOTCompilerMode,
                msBuildMoniker: msBuildMoniker,
                moniker: moniker);

            var toolChain = MonoAotLLVMToolChain.From(
                new NetCoreAppSettings(
                    targetFrameworkMoniker: monoAotLLVMRuntime.MsBuildMoniker,
                    runtimeFrameworkVersion: "",
                    name: monoAotLLVMRuntime.Name,
                    options: options));

            return baseJob.WithRuntime(monoAotLLVMRuntime).WithToolchain(toolChain).WithId(monoAotLLVMRuntime.Name);
        }

        private static Job CreateR2RJob(Job baseJob, CommandLineOptions options, Runtime runtime)
        {
            var toolChain = R2RToolchain.From(
                new NetCoreAppSettings(
                    targetFrameworkMoniker: runtime.MsBuildMoniker,
                    runtimeFrameworkVersion: "",
                    name: runtime.Name,
                    options: options));

            return baseJob.WithRuntime(runtime).WithToolchain(toolChain).WithId(runtime.Name);
        }

        private static Job MakeWasmJob(Job baseJob, CommandLineOptions options, string msBuildMoniker, RuntimeMoniker moniker)
        {
            bool wasmAot = options.AOTCompilerMode == MonoAotCompilerMode.wasm;

            var wasmRuntime = new WasmRuntime(
                msBuildMoniker: msBuildMoniker,
                moniker: moniker,
                displayName: "Wasm",
                javaScriptEngine: options.WasmJavaScriptEngine,
                javaScriptEngineArguments: options.WasmJavaScriptEngineArguments,
                aot: wasmAot,
                runtimeFlavor: options.WasmRuntimeFlavor,
                mainJsTemplate: options.WasmMainJsTemplate,
                processTimeoutMinutes: options.WasmProcessTimeoutMinutes);

            var toolChain = WasmToolchain.From(new NetCoreAppSettings(
                targetFrameworkMoniker: wasmRuntime.MsBuildMoniker,
                runtimeFrameworkVersion: "",
                name: wasmRuntime.Name,
                options: options));

            return baseJob.WithRuntime(wasmRuntime).WithToolchain(toolChain).WithId(wasmRuntime.Name);
        }

        private static IEnumerable<IFilter> GetFilters(CommandLineOptions options)
        {
            if (options.Filters.Any())
                yield return new GlobFilter(options.Filters.ToArray());
            if (options.AllCategories.Any())
                yield return new AllCategoriesFilter(options.AllCategories.ToArray());
            if (options.AnyCategories.Any())
                yield return new AnyCategoriesFilter(options.AnyCategories.ToArray());
            if (options.AnyJobCategories.Any())
                yield return new JobCategoryFilter(options.AnyJobCategories.ToArray());
            if (options.AttributeNames.Any())
                yield return new AttributesFilter(options.AttributeNames.ToArray());
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

    file static class ExtensionMethods
    {
        public static void AddValue(this List<string> list, string key, string value)
        {
            list.Add(key);
            list.Add(value);
        }

        public static void AddValues(this List<string> list, string key, IEnumerable<string> values)
        {
            list.Add(key);
            list.AddRange(values);
        }
    }
}
