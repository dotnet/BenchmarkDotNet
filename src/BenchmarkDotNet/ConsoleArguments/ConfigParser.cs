using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
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
using BenchmarkDotNet.Toolchains.R2R;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.NativeAot;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using BenchmarkDotNet.Toolchains.Mono;
using Perfolizer.Metrology;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;
using System.CommandLine.Invocation;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

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
                { "rplot", new[] { CsvMeasurementsExporter.Default, RPlotExporter.Default } },
                { "json", new[] { JsonExporter.Default } },
                { "briefjson", new[] { JsonExporter.Brief } },
                { "fulljson", new[] { JsonExporter.Full } },
                { "asciidoc", new[] { AsciiDocExporter.Default } },
                { "xml", new[] { XmlExporter.Default } },
                { "briefxml", new[] { XmlExporter.Brief } },
                { "fullxml", new[] { XmlExporter.Full } }
            };


        internal static RootCommand RootCommand
        {
            get
            {
                using var invariantUICultureScope = Helpers.CultureInfoHelper.CreateInvariantUICultureScope();

                return new RootCommand("BenchmarkDotNet Command Line options")
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
                    CommandLineOptions.WasmJavascriptEngineOption,
                    CommandLineOptions.WasmJavaScriptEngineArgumentsOption,
                    CommandLineOptions.CustomRuntimePackOption,
                    CommandLineOptions.AOTCompilerPathOption,
                    CommandLineOptions.AOTCompilerModeOption,
                    CommandLineOptions.WasmDataDirectoryOption,
                    CommandLineOptions.WasmCoreCLROption,
                    CommandLineOptions.NoForcedGCsOption,
                    CommandLineOptions.NoEvaluationOverheadOption,
                    CommandLineOptions.ResumeOption,
                };
            }
        }

        private static bool HasDuplicateOptions(string[] args)
        {
            var aliasToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["-j"] = "--job",
                ["-r"] = "--runtimes",
                ["-e"] = "--exporters",
                ["-m"] = "--memory",
                ["-t"] = "--threading",
                ["-d"] = "--disasm",
                ["-p"] = "--profiler",
                ["-f"] = "--filter",
                ["-h"] = "--hide",
                ["-i"] = "--inprocess",
                ["-a"] = "--artifacts"
            };

            var options = args.Where(a => a.StartsWith("-") && a != "--")
                              .Select(a => a.Split('=')[0].ToLowerInvariant())
                              .Select(a => aliasToCanonical.TryGetValue(a, out var c) ? c : a);

            return options.GroupBy(x => x).Any(g => g.Count() > 1);
        }

        private static string[] NormalizeArgs(string[] args)
        {
            var aliasToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
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
                ["--runOncePerIteration"] = "--runOncePerIteration",
                ["--runoncperiteration"] = "--runOncePerIteration",
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
                ["--wasmengine"] = "--wasmEngine",
                ["--wasmargs"] = "--wasmArgs",
                ["--customruntimepack"] = "--customRuntimePack",
                ["--aotcompilerpath"] = "--AOTCompilerPath",
                ["--aotcompilermode"] = "--AOTCompilerMode",
                ["--wasmdatadir"] = "--wasmDataDir",
                ["--wasmcoreclr"] = "--wasmCoreCLR",
                ["--noforcedgcs"] = "--noForcedGCs",
                ["--nooverheadevaluation"] = "--noOverheadEvaluation",
                ["--resume"] = "--resume",
            };

            var result = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "--")
                {
                    result.Add(arg);
                    for (int j = i + 1; j < args.Length; j++)
                        result.Add(args[j]);
                    break;
                }

                if (arg.StartsWith("-"))
                {
                    var eqIdx = arg.IndexOf('=');
                    string key = eqIdx >= 0 ? arg.Substring(0, eqIdx) : arg;
                    string? value = eqIdx >= 0 ? arg.Substring(eqIdx + 1) : null;

                    if (aliasToCanonical.TryGetValue(key, out var canonical))
                        key = canonical;

                    if (key.Equals("--counters", StringComparison.OrdinalIgnoreCase))
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
                }

                result.Add(arg);
            }

            return result.ToArray();
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
                return (false, default, default);
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

            var options = new CommandLineOptions
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
                ILCompilerVersion = parseResult.GetValue(CommandLineOptions.ILCompilerVersionOption),
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
                WasmJavascriptEngine = parseResult.GetValue(CommandLineOptions.WasmJavascriptEngineOption),
                WasmJavaScriptEngineArguments = parseResult.GetValue(CommandLineOptions.WasmJavaScriptEngineArgumentsOption),
                CustomRuntimePack = parseResult.GetValue(CommandLineOptions.CustomRuntimePackOption),
                AOTCompilerPath = parseResult.GetValue(CommandLineOptions.AOTCompilerPathOption),
                AOTCompilerMode = parseResult.GetValue(CommandLineOptions.AOTCompilerModeOption),
                WasmDataDirectory = parseResult.GetValue(CommandLineOptions.WasmDataDirectoryOption),
                WasmCoreCLR = parseResult.GetValue(CommandLineOptions.WasmCoreCLROption),
                NoForcedGCs = parseResult.GetValue(CommandLineOptions.NoForcedGCsOption),
                NoEvaluationOverhead = parseResult.GetValue(CommandLineOptions.NoEvaluationOverheadOption),
                Resume = parseResult.GetValue(CommandLineOptions.ResumeOption),
            };

            bool isSuccess = Validate(options, logger);
            return isSuccess
                ? (true, CreateConfig(options, globalConfig, args), options)
                : (false, default, default);
        }

        private static (bool Success, string[] ExpandedTokens) ExpandResponseFile(string[] args, ILogger logger)
        {
            List<string> result = new();
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

            var options = new CommandLineOptions
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
                ILCompilerVersion = parseResult.GetValue(CommandLineOptions.ILCompilerVersionOption),
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
                WasmJavascriptEngine = parseResult.GetValue(CommandLineOptions.WasmJavascriptEngineOption),
                WasmJavaScriptEngineArguments = parseResult.GetValue(CommandLineOptions.WasmJavaScriptEngineArgumentsOption),
                CustomRuntimePack = parseResult.GetValue(CommandLineOptions.CustomRuntimePackOption),
                AOTCompilerPath = parseResult.GetValue(CommandLineOptions.AOTCompilerPathOption),
                AOTCompilerMode = parseResult.GetValue(CommandLineOptions.AOTCompilerModeOption),
                WasmDataDirectory = parseResult.GetValue(CommandLineOptions.WasmDataDirectoryOption),
                WasmCoreCLR = parseResult.GetValue(CommandLineOptions.WasmCoreCLROption),
                NoForcedGCs = parseResult.GetValue(CommandLineOptions.NoForcedGCsOption),
                NoEvaluationOverhead = parseResult.GetValue(CommandLineOptions.NoEvaluationOverheadOption),
                Resume = parseResult.GetValue(CommandLineOptions.ResumeOption),
            };

            if (invalidOptions.Any() || !Validate(options, NullLogger.Instance))
            {
                updatedArgs = null;
                return false;
            }

            updater(options);

            updatedArgs = SerializeToArgs(options);
            return true;
        }

        private static string[] SerializeToArgs(CommandLineOptions options)
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
            if (options.WasmCoreCLR) result.Add("--wasmCoreCLR");
            if (options.NoForcedGCs) result.Add("--noForcedGCs");
            if (options.NoEvaluationOverhead) result.Add("--noOverheadEvaluation");
            if (options.Resume) result.Add("--resume");

            if (options.Runtimes.Any())
            {
                result.Add("--runtimes");
                result.AddRange(options.Runtimes);
            }

            if (options.Profiler.IsNotBlank())
            { result.Add("--profiler"); result.Add(options.Profiler); }

            if (options.ClrVersion.IsNotBlank())
            { result.Add("--clrVersion"); result.Add(options.ClrVersion); }

            if (options.StatisticalTestThreshold.IsNotBlank())
            { result.Add("--statisticalTest"); result.Add(options.StatisticalTestThreshold); }

            if (options.ILCompilerVersion.IsNotBlank())
            { result.Add("--ilCompilerVersion"); result.Add(options.ILCompilerVersion!); }

            if (options.CustomRuntimePack.IsNotBlank())
            { result.Add("--customRuntimePack"); result.Add(options.CustomRuntimePack!); }

            if (options.WasmJavaScriptEngineArguments != null
                && options.WasmJavaScriptEngineArguments != "--expose_wasm")
            { result.Add("--wasmArgs"); result.Add(options.WasmJavaScriptEngineArguments); }

            if (options.Exporters.Any())
            { result.Add("--exporters"); result.AddRange(options.Exporters); }

            if (options.HardwareCounters.Any())
            { result.Add("--counters"); result.AddRange(options.HardwareCounters); }

            if (options.AllCategories.Any())
            { result.Add("--allCategories"); result.AddRange(options.AllCategories); }

            if (options.AnyCategories.Any())
            { result.Add("--anyCategories"); result.AddRange(options.AnyCategories); }

            if (options.AttributeNames.Any())
            { result.Add("--attribute"); result.AddRange(options.AttributeNames); }

            if (options.HiddenColumns.Any())
            { result.Add("--hide"); result.AddRange(options.HiddenColumns); }

            if (options.DisassemblerFilters.Any())
            { result.Add("--disasmFilter"); result.AddRange(options.DisassemblerFilters); }

            if (options.EnvironmentVariables.Any())
            { result.Add("--envVars"); result.AddRange(options.EnvironmentVariables); }

            if (options.LaunchCount.HasValue)
            { result.Add("--launchCount"); result.Add(options.LaunchCount.Value.ToString()); }

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

            if (options.WasmJavascriptEngine != null)
            { result.Add("--wasmEngine"); result.Add(options.WasmJavascriptEngine.FullName); }

            if (options.WasmDataDirectory != null)
            { result.Add("--wasmDataDir"); result.Add(options.WasmDataDirectory.FullName); }

            if (options.AOTCompilerPath != null)
            { result.Add("--AOTCompilerPath"); result.Add(options.AOTCompilerPath.FullName); }

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
                if (!TryParse(runtime, out RuntimeMoniker runtimeMoniker))
                {
                    logger.WriteLineError($"The provided runtime \"{runtime}\" is invalid. Available options are: {string.Join(", ", Enum.GetNames(typeof(RuntimeMoniker)).Select(name => name.ToLower()))}.");
                    return false;
                }
                else if (runtimeMoniker == RuntimeMoniker.MonoAOTLLVM && (options.AOTCompilerPath == null || options.AOTCompilerPath.IsNotNullButDoesNotExist()))
                {
                    logger.WriteLineError($"The provided {nameof(options.AOTCompilerPath)} \"{options.AOTCompilerPath}\" does NOT exist. It MUST be provided.");
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

            if (options.WasmJavascriptEngine.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"The provided {nameof(options.WasmJavascriptEngine)} \"{options.WasmJavascriptEngine}\" does NOT exist.");
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

            if (options.StatisticalTestThreshold.IsNotBlank() && !Threshold.TryParse(options.StatisticalTestThreshold, out _))
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

        private static IConfig CreateConfig(CommandLineOptions options, IConfig? globalConfig, string[] args)
        {
            var config = new ManualConfig();

            var baseJob = GetBaseJob(options, globalConfig);
            var expanded = Expand(baseJob.UnfreezeCopy(), options, args).ToArray();
            if (expanded.Length > 1)
                expanded[0] = expanded[0].AsBaseline();
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
                globalConfig?.GetJobs().SingleOrDefault(job => job.Meta.IsDefault)
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
            if (options.NoForcedGCs)
                baseJob = baseJob.WithGcForce(false);
            if (options.NoEvaluationOverhead)
#pragma warning disable CS0618
                baseJob = baseJob.WithEvaluateOverhead(false);
#pragma warning restore CS0618

            if (options.EnvironmentVariables.Any())
            {
                baseJob = baseJob.WithEnvironmentVariables(options.EnvironmentVariables.Select(text =>
                {
                    var separated = text.Split(new[] { EnvVarKeyValueSeparator }, 2);
                    return new EnvironmentVariable(separated[0], separated[1]);
                }).ToArray());
            }

            if (AvailableJobs.Values.Contains(baseJob))
                return baseJob;

            return baseJob
                .AsDefault(false)
                .AsMutator();
        }

        private static IEnumerable<Job> Expand(Job baseJob, CommandLineOptions options, string[] args)
        {
            if (options.RunInProcess)
            {
                yield return Attributes.InProcessAttribute.GetJob(Attributes.InProcessToolchainType.Auto, true);
            }
            else if (options.ClrVersion.IsNotBlank())
            {
                var runtime = ClrRuntime.CreateForLocalFullNetFrameworkBuild(options.ClrVersion);
                yield return baseJob.WithRuntime(runtime).WithId(runtime.Name);
            }
            else if (options.CliPath != null && options.Runtimes.IsEmpty() && options.CoreRunPaths.IsEmpty())
            {
                yield return CreateCoreJobWithCli(baseJob, options);
            }
            else
            {
                string? first = args.FirstOrDefault(arg =>
                    arg.Equals("--runtimes", StringComparison.OrdinalIgnoreCase)
                    || arg.Equals("-r", StringComparison.OrdinalIgnoreCase)
                    || arg.Equals("--corerun", StringComparison.OrdinalIgnoreCase));

                if (first is null || first.Equals("--corerun", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var coreRunPath in options.CoreRunPaths)
                        yield return CreateCoreRunJob(baseJob, options, coreRunPath);

                    foreach (string runtime in options.Runtimes)
                        yield return CreateJobForGivenRuntime(baseJob, runtime, options);
                }
                else
                {
                    foreach (string runtime in options.Runtimes)
                        yield return CreateJobForGivenRuntime(baseJob, runtime, options);

                    foreach (var coreRunPath in options.CoreRunPaths)
                        yield return CreateCoreRunJob(baseJob, options, coreRunPath);
                }
            }
        }

        private static Job CreateJobForGivenRuntime(Job baseJob, string runtimeId, CommandLineOptions options)
        {
            if (!TryParse(runtimeId, out RuntimeMoniker runtimeMoniker))
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
                case RuntimeMoniker.Net481:
                    {
                        var runtime = runtimeMoniker.GetRuntime();
                        return baseJob
                            .WithRuntime(runtime)
                            .WithId(runtime.Name)
                            .WithToolchain(CsProjClassicNetToolchain.From(runtimeId, options.RestorePath?.FullName ?? "", options.CliPath?.FullName ?? ""));
                    }

                case RuntimeMoniker.NetCoreApp20:
                case RuntimeMoniker.NetCoreApp21:
                case RuntimeMoniker.NetCoreApp22:
                case RuntimeMoniker.NetCoreApp30:
                case RuntimeMoniker.NetCoreApp31:
                case RuntimeMoniker.Net50:
                case RuntimeMoniker.Net60:
                case RuntimeMoniker.Net70:
                case RuntimeMoniker.Net80:
                case RuntimeMoniker.Net90:
                case RuntimeMoniker.Net10_0:
                case RuntimeMoniker.Net11_0:
                    {
                        var runtime = runtimeMoniker.GetRuntime();
                        return baseJob
                            .WithRuntime(runtime)
                            .WithId(runtime.Name)
                            .WithToolchain(CsProjCoreToolchain.From(
                                new NetCoreAppSettings(
                                    runtimeId,
                                    runtimeFrameworkVersion: "",
                                    name: runtimeId,
                                    options: options)));
                    }

                case RuntimeMoniker.Mono:
                    {
                        var runtime = new MonoRuntime("Mono", options.MonoPath?.FullName ?? "");
                        return baseJob.WithRuntime(runtime).WithId(runtime.Name);
                    }

                case RuntimeMoniker.NativeAot60:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "6.0.0-*", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json");
                case RuntimeMoniker.NativeAot70:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "", "https://api.nuget.org/v3/index.json");
                case RuntimeMoniker.NativeAot80:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "", "https://api.nuget.org/v3/index.json");
                case RuntimeMoniker.NativeAot90:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json");
                case RuntimeMoniker.NativeAot10_0:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json");
                case RuntimeMoniker.NativeAot11_0:
                    return CreateAotJob(baseJob, options, runtimeMoniker, "", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json");

                case RuntimeMoniker.WasmNet80:
                    return MakeWasmJob(baseJob, options, "net8.0", runtimeMoniker);
                case RuntimeMoniker.WasmNet90:
                    return MakeWasmJob(baseJob, options, "net9.0", runtimeMoniker);
                case RuntimeMoniker.WasmNet10_0:
                    return MakeWasmJob(baseJob, options, "net10.0", runtimeMoniker);
                case RuntimeMoniker.WasmNet11_0:
                    return MakeWasmJob(baseJob, options, "net11.0", runtimeMoniker);

                case RuntimeMoniker.MonoAOTLLVM:
                    return MakeMonoAOTLLVMJob(baseJob, options, RuntimeInformation.IsNetCore ? CoreRuntime.GetCurrentVersion().MsBuildMoniker : "net6.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet60:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net6.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet70:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net7.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet80:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net8.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet90:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net9.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet10_0:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net10.0", runtimeMoniker);
                case RuntimeMoniker.MonoAOTLLVMNet11_0:
                    return MakeMonoAOTLLVMJob(baseJob, options, "net11.0", runtimeMoniker);

                case RuntimeMoniker.Mono60:
                    return MakeMonoJob(baseJob, options, MonoRuntime.Mono60);
                case RuntimeMoniker.Mono70:
                    return MakeMonoJob(baseJob, options, MonoRuntime.Mono70);
                case RuntimeMoniker.Mono80:
                    return MakeMonoJob(baseJob, options, MonoRuntime.Mono80);

                case RuntimeMoniker.R2R80:
                case RuntimeMoniker.R2R90:
                case RuntimeMoniker.R2R10_0:
                case RuntimeMoniker.R2R11_0:
                    return CreateR2RJob(baseJob, options, runtimeMoniker.GetRuntime());

                default:
                    throw new NotSupportedException($"Runtime {runtimeId} is not supported");
            }
        }

        private static Job CreateAotJob(Job baseJob, CommandLineOptions options, RuntimeMoniker runtimeMoniker, string ilCompilerVersion, string nuGetFeedUrl)
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
                javaScriptEngine: options.WasmJavascriptEngine?.FullName ?? "v8",
                javaScriptEngineArguments: options.WasmJavaScriptEngineArguments ?? "",
                aot: wasmAot,
                wasmDataDir: options.WasmDataDirectory?.FullName ?? "",
                moniker: moniker,
                isMonoRuntime: !options.WasmCoreCLR);

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
            if (options.AttributeNames.Any())
                yield return new AttributesFilter(options.AttributeNames.ToArray());
        }

        private static Job CreateCoreRunJob(Job baseJob, CommandLineOptions options, FileInfo coreRunPath)
            => baseJob
                .WithToolchain(new CoreRunToolchain(
                    coreRunPath,
                    createCopy: true,
                    targetFrameworkMoniker:
                        RuntimeInformation.IsNetCore
                            ? RuntimeInformation.GetCurrentRuntime().MsBuildMoniker
                            : CoreRuntime.Latest.MsBuildMoniker,
                    customDotNetCliPath: options.CliPath,
                    restorePath: options.RestorePath,
                    displayName: GetCoreRunToolchainDisplayName(options.CoreRunPaths, coreRunPath)));

        private static Job CreateCoreJobWithCli(Job baseJob, CommandLineOptions options)
            => baseJob
                .WithToolchain(CsProjCoreToolchain.From(
                    new NetCoreAppSettings(
                        targetFrameworkMoniker: RuntimeInformation.GetCurrentRuntime().MsBuildMoniker,
                        runtimeFrameworkVersion: "",
                        name: RuntimeInformation.GetCurrentRuntime().Name,
                        options: options)));

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

        internal static bool TryParse(string runtime, out RuntimeMoniker runtimeMoniker)
        {
            int index = runtime.IndexOf('-');
            if (index >= 0)
            {
                runtime = runtime.Substring(0, index);
            }

            if (Enum.TryParse(runtime.Replace(".", string.Empty), ignoreCase: true, out runtimeMoniker))
            {
                return true;
            }
            return Enum.TryParse(runtime.Replace('.', '_'), ignoreCase: true, out runtimeMoniker);
        }
    }
}