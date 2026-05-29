using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using Perfolizer.Mathematics.OutlierDetection;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.ConsoleArguments
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class CommandLineOptions
    {
        private const int DefaultDisassemblerRecursiveDepth = 1;
        private bool useDisassemblyDiagnoser;

        public string BaseJob { get; set; } = "";
        public static readonly Option<string> BaseJobOption = new("--job", "-j")
        {
            DefaultValueFactory = _ => "Default",
            Description = "Dry/Short/Medium/Long or Default",
        };

        public IEnumerable<string> Runtimes { get; set; } = [];
        public static readonly Option<string[]> RuntimesOption = new("--runtimes", "-r")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Full target framework moniker for .NET Core and .NET. For Mono just 'Mono'. For NativeAOT please append target runtime version (example: 'nativeaot7.0'). First one will be marked as baseline!",
        };

        public IEnumerable<string> Exporters { get; set; } = [];
        public static readonly Option<string[]> ExportersOption = new("--exporters", "-e")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML/CSVMeasurements/Markdown/Atlassian/Plain/BriefJSON/FullJSON/Asciidoc/BriefXML/FullXML/OpenMetrics",
        };

        public bool UseMemoryDiagnoser { get; set; }
        public static readonly Option<bool> MemoryOption = new("--memory", "-m")
        {
            Description = "Prints memory statistics",
        };

        public bool UseThreadingDiagnoser { get; set; }
        public static readonly Option<bool> ThreadingOption = new("--threading", "-t")
        {
            Description = "Prints threading statistics",
        };

        public bool UseExceptionDiagnoser { get; set; }
        public static readonly Option<bool> ExceptionsOption = new("--exceptions")
        {
            Description = "Prints exception statistics",
        };

        public bool UseDisassemblyDiagnoser
        {
            get => useDisassemblyDiagnoser || DisassemblerRecursiveDepth != DefaultDisassemblerRecursiveDepth || DisassemblerFilters.Any();
            set => useDisassemblyDiagnoser = value;
        }
        public static readonly Option<bool> DisassemblyOption = new("--disasm", "-d")
        {
            Description = "Gets disassembly of benchmarked code",
        };

        public string Profiler { get; set; } = "";
        public static readonly Option<string> ProfilerOption = new("--profiler", "-p")
        {
            Description = "Profiles benchmarked code using selected profiler. Available options: EP/ETW/CV/NativeMemory",
        };

        public IEnumerable<string> Filters { get; set; } = [];
        public static readonly Option<string[]> FiltersOption = new("--filter", "-f")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Glob patterns",
        };

        public IEnumerable<string> HiddenColumns { get; set; } = [];
        public static readonly Option<string[]> HiddenColumnsOption = new("--hide", "-h")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Hides columns by name",
        };

        public bool RunInProcess { get; set; }
        public static readonly Option<bool> RunInProcessOption = new("--inProcess", "-i")
        {
            Description = "Run benchmarks in Process",
        };
        public DirectoryInfo? ArtifactsDirectory { get; set; }
        public static readonly Option<DirectoryInfo> ArtifactsDirectoryOption = new("--artifacts", "-a")
        {
            Description = "Valid path to accessible directory",
        };

        public OutlierMode Outliers { get; set; }
        public static readonly Option<OutlierMode> OutliersOption = new("--outliers")
        {
            DefaultValueFactory = _ => OutlierMode.RemoveUpper,
            Description = "DontRemove/RemoveUpper/RemoveLower/RemoveAll",
        };

        public int? Affinity { get; set; }
        public static readonly Option<int?> AffinityOption = new("--affinity")
        {
            Description = "Affinity mask to set for the benchmark process",
        };

        public bool DisplayAllStatistics { get; set; }
        public static readonly Option<bool> DisplayAllStatisticsOption = new("--allStats")
        {
            Description = "Displays all statistics (min, max & more)",
        };

        public IEnumerable<string> AllCategories { get; set; } = [];
        public static readonly Option<string[]> AllCategoriesOption = new("--allCategories")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed",
        };

        public IEnumerable<string> AnyCategories { get; set; } = [];
        public static readonly Option<string[]> AnyCategoriesOption = new("--anyCategories")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Any Categories to run",
        };

        public IEnumerable<string> AttributeNames { get; set; } = [];
        public static readonly Option<string[]> AttributeNamesOption = new("--attribute")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Run all methods with given attribute (applied to class or method)",
        };

        public bool Join { get; set; }
        public static readonly Option<bool> JoinOption = new("--join")
        {
            Description = "Prints single table with results for all benchmarks"
        };

        public bool KeepBenchmarkFiles { get; set; }
        public static readonly Option<bool> KeepBenchmarkFilesOption = new("--keepFiles")
        {
            Description = "Determines if all auto-generated files should be kept or removed after running the benchmarks.",
        };

        public bool DontOverwriteResults { get; set; }
        public static readonly Option<bool> DontOverwriteResultsOption = new("--noOverwrite")
        {
            Description = "Determines if the exported result files should not be overwritten (by default they are overwritten)."
        };

        public IEnumerable<string> HardwareCounters { get; set; } = [];
        public static readonly Option<string[]> HardwareCountersOption = new("--counters")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Hardware Counters",
        };

        public FileInfo? CliPath { get; set; }
        public static readonly Option<FileInfo> CliPathOption = new("--cli")
        {
            Description = "Path to dotnet cli (optional).",
        };

        public DirectoryInfo? RestorePath { get; set; }
        public static readonly Option<FileInfo> RestorePathOption = new("--packages")
        {
            Description = "The directory to restore packages to (optional).",
        };

        public IReadOnlyList<FileInfo> CoreRunPaths { get; set; } = [];
        public static readonly Option<FileInfo[]> CoreRunPathsOption = new("--coreRun")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Path(s) to CoreRun (optional).",
        };

        public FileInfo? MonoPath { get; set; }
        public static readonly Option<FileInfo> MonoPathOption = new("--monoPath")
        {
            Description = "Optional path to Mono which should be used for running benchmarks.",
        };

        public string ClrVersion { get; set; } = "";
        public static readonly Option<string> ClrVersionOption = new("--clrVersion")
        {
            Description = "Optional version of private CLR build used as the value of COMPLUS_Version env var.",
        };

        public string ILCompilerVersion { get; set; } = "";
        public static readonly Option<string> ILCompilerVersionOption = new("--ilCompilerVersion")
        {
            Description = "Optional version of Microsoft.DotNet.ILCompiler which should be used to run with NativeAOT. Example: \"7.0.0-preview.3.22123.2\"",
        };

        public DirectoryInfo? IlcPackages { get; set; }
        public static readonly Option<DirectoryInfo> IlcPackagesOption = new("--ilcPackages")
        {
            Description = "Optional path to shipping packages produced by local dotnet/runtime build.",
        };

        public int? LaunchCount { get; set; }
        public static readonly Option<int?> LaunchCountOption = new("--launchCount")
        {
            Description = "How many times we should launch process with target benchmark. The default is 1."
        };

        public int? WarmupIterationCount { get; set; }
        public static readonly Option<int?> WarmupCountOption = new("--warmupCount")
        {
            Description = "How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.",
        };

        public int? MinWarmupIterationCount { get; set; }
        public static readonly Option<int?> MinWarmupCountOption = new("--minWarmupCount")
        {
            Description = "Minimum count of warmup iterations that should be performed. The default is 6.",
        };

        public int? MaxWarmupIterationCount { get; set; }
        public static readonly Option<int?> MaxWarmupCountOption = new("--maxWarmupCount")
        {
            Description = "Maximum count of warmup iterations that should be performed. The default is 50."
        };

        public int? IterationTimeInMilliseconds { get; set; }
        public static readonly Option<int?> IterationTimeOption = new("--iterationTime")
        {
            Description = "Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default",
        };

        public int? IterationCount { get; set; }
        public static readonly Option<int?> IterationCountOption = new("--iterationCount")
        {
            Description = "How many target iterations should be performed. By default calculated by the heuristic.",
        };

        public int? MinIterationCount { get; set; }
        public static readonly Option<int?> MinIterationCountOption = new("--minIterationCount")
        {
            Description = "Minimum number of iterations to run. The default is 15.",
        };

        public int? MaxIterationCount { get; set; }
        public static readonly Option<int?> MaxIterationCountOption = new("--maxIterationCount")
        {
            Description = "Maximum number of iterations to run. The default is 100.",
        };

        public long? InvocationCount { get; set; }
        public static readonly Option<long?> InvocationCountOption = new("--invocationCount")
        {
            Description = "Invocation count in a single iteration. By default calculated by the heuristic.",
        };

        public int? UnrollFactor { get; set; }
        public static readonly Option<int?> UnrollFactorOption = new("--unrollFactor")
        {
            Description = "How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default",
        };

        public RunStrategy? RunStrategy { get; set; }
        public static readonly Option<RunStrategy?> RunStrategyOption = new("--strategy")
        {
            Description = "The RunStrategy that should be used. Throughput/ColdStart/Monitoring.",
        };

        public Platform? Platform { get; set; }
        public static readonly Option<Platform?> PlatformOption = new("--platform")
        {
            Description = "The Platform that should be used. If not specified, the host process platform is used (default). AnyCpu/X86/X64/Arm/Arm64/LoongArch64.",
        };

        public bool RunOncePerIteration { get; set; }
        public static readonly Option<bool> RunOnceOption = new("--runOncePerIteration")
        {
            Description = "Run the benchmark exactly once per iteration.",
        };

        public bool PrintInformation { get; set; }
        public static readonly Option<bool> PrintInformationOption = new("--info")
        {
            Description = "Print environment information.",
        };

        public bool ApplesToApples { get; set; }
        public static readonly Option<bool> ApplesToApplesOption = new("--apples")
        {
            Description = "Runs apples-to-apples comparison for specified Jobs.",
        };

        public ListBenchmarkCaseMode ListBenchmarkCaseMode { get; set; }
        public static readonly Option<ListBenchmarkCaseMode> ListBenchmarkCaseModeOption = new("--list")
        {
            DefaultValueFactory = _ => ListBenchmarkCaseMode.Disabled,
            Description = "Prints all of the available benchmark names. Flat/Tree",
        };

        public int DisassemblerRecursiveDepth { get; set; }
        public static readonly Option<int> DisassemblerDepthOption = new("--disasmDepth")
        {
            DefaultValueFactory = _ => DefaultDisassemblerRecursiveDepth,
            Description = "Sets the recursive depth for the disassembler.",
        };

        public IEnumerable<string> DisassemblerFilters { get; set; } = [];
        public static readonly Option<string[]> DisassemblerFiltersOption = new("--disasmFilter")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Glob patterns applied to full method signatures by the disassembler.",
        };

        public bool DisassemblerDiff { get; set; }
        public static readonly Option<bool> DisassemblerDiffOption = new("--disasmDiff")
        {
            Description = "Generates diff reports for the disassembler.",
        };

        public bool LogBuildOutput { get; set; }
        public static readonly Option<bool> LogBuildOutputOption = new("--logBuildOutput")
        {
            Description = "Log Build output.",
        };

        public bool GenerateMSBuildBinLog { get; set; }
        public static readonly Option<bool> GenerateBinLogOption = new("--generateBinLog")
        {
            Description = "Generate msbuild binlog for builds",
        };

        public int? TimeOutInSeconds { get; set; }
        public static readonly Option<int?> TimeoutOption = new("--buildTimeout")
        {
            Description = "Build timeout in seconds.",
        };

        public WakeLockType? WakeLock { get; set; }
        public static readonly Option<WakeLockType?> WakeLockOption = new("--wakeLock")
        {
            Description = "Prevents the system from entering sleep or turning off the display. None/System/Display.",
        };

        public bool StopOnFirstError { get; set; }
        public static readonly Option<bool> StopOnFirstErrorOption = new("--stopOnFirstError")
        {
            Description = "Stop on first error.",
        };

        public string StatisticalTestThreshold { get; set; } = "";
        public static readonly Option<string> StatisticalTestThresholdOption = new("--statisticalTest")
        {
            Description = "Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s. Bare numbers imply ns (e.g. 0.02 -> 0.02ns)",
        };

        public bool DisableLogFile { get; set; }
        public static readonly Option<bool> DisableLogFileOption = new("--disableLogFile")
        {
            Description = "Disables the logfile.",
        };

        public int? MaxParameterColumnWidth { get; set; }
        public static readonly Option<int?> MaxParameterColumnWidthOption = new("--maxWidth")
        {
            Description = "Max parameter column width, the default is 20.",
        };

        public IEnumerable<string> EnvironmentVariables { get; set; } = [];
        public static readonly Option<string[]> EnvironmentVariablesOption = new("--envVars")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Colon separated environment variables (key:value)",
        };

        public bool MemoryRandomization { get; set; }
        public static readonly Option<bool> MemoryRandomizationOption = new("--memoryRandomization")
        {
            Description = "Specifies whether Engine should allocate some random-sized memory between iterations.",
        };

        public string WasmJavaScriptEngine { get; set; } = "";
        public static readonly Option<string> WasmJavascriptEngineOption = new("--wasmEngine")
        {
            DefaultValueFactory = _ => "v8",
            Description = "Specifies the executable (in PATH) or full path to a java script engine used to run the benchmarks, used by Wasm toolchain.",
        };

        public string WasmJavaScriptEngineArguments { get; set; } = "";
        public static readonly Option<string> WasmJavaScriptEngineArgumentsOption = new("--wasmArgs")
        {
            DefaultValueFactory = _ => "--expose_wasm",
            Description = "Arguments for the javascript engine used by Wasm toolchain.",
        };

        public FileInfo? WasmMainJsTemplate { get; set; }
        public static readonly Option<FileInfo> WasmMainJsTemplateOption = new("--wasmMainJsTemplate")
        {
            Description = "Path to main.mjs template.",
        };

        public string? CustomRuntimePack { get; set; }
        public static readonly Option<string> CustomRuntimePackOption = new("--customRuntimePack")
        {
            Description = "Path to a custom runtime pack. Only used for wasm/MonoAotLLVM currently.",
        };

        public FileInfo? AOTCompilerPath { get; set; }
        public static readonly Option<FileInfo> AOTCompilerPathOption = new("--AOTCompilerPath")
        {
            Description = "Path to Mono AOT compiler, used for MonoAotLLVM.",
        };

        public MonoAotCompilerMode AOTCompilerMode { get; set; }
        public static readonly Option<MonoAotCompilerMode> AOTCompilerModeOption = new("--AOTCompilerMode")
        {
            DefaultValueFactory = _ => MonoAotCompilerMode.mini,
            Description = "Mono AOT compiler mode, either 'mini' or 'llvm'",
        };

        public RuntimeFlavor WasmRuntimeFlavor { get; set; }
        public static readonly Option<RuntimeFlavor> WasmRuntimeFlavorOption = new("--wasmRuntimeFlavor")
        {
            DefaultValueFactory = _ => RuntimeFlavor.Mono,
            Description = "Runtime flavor for WASM benchmarks: 'Mono' (default) uses the Mono runtime pack, 'CoreCLR' uses the CoreCLR runtime pack.",
        };

        public int WasmProcessTimeoutMinutes { get; set; }
        public static readonly Option<int> WasmProcessTimeoutMinutesOption = new("--wasmProcessTimeout")
        {
            DefaultValueFactory = _ => 10,
            Description = "Maximum time in minutes to wait for a single WASM benchmark process to finish before force killing it.",
        };

        public bool NoForcedGCs { get; set; }
        public static readonly Option<bool> NoForcedGCsOption = new("--noForcedGCs")
        {
            Description = "Specifying would not forcefully induce any GCs.",
        };

        public bool EvaluateOverhead { get; set; }
        public static readonly Option<bool> EvaluateOverheadOption = new("--evaluateOverhead")
        {
            Description = "Specifying would not run the evaluation overhead iterations.",
        };

        public bool Resume { get; set; }
        public static readonly Option<bool> ResumeOption = new("--resume")
        {
            Description = "Continue the execution if the last run was stopped.",
        };

        public string[] ExtraArguments { get; set; } = [];

        internal bool UserProvidedFilters
            => Filters.Any()
            || AttributeNames.Any()
            || AllCategories.Any()
            || AnyCategories.Any();

        static CommandLineOptions()
        {
            // Set validators for options that enable AllowMultipleArgumentsPerToken
            AddUnrecognizedValidator(RuntimesOption);
            AddUnrecognizedValidator(ExportersOption);
            AddUnrecognizedValidator(FiltersOption);
            AddUnrecognizedValidator(AllCategoriesOption);
            AddUnrecognizedValidator(AnyCategoriesOption);
            AddUnrecognizedValidator(AttributeNamesOption);
            AddUnrecognizedValidator(HiddenColumnsOption);
            AddUnrecognizedValidator(HardwareCountersOption);
            AddUnrecognizedValidator(EnvironmentVariablesOption);
            AddUnrecognizedValidator(DisassemblerFiltersOption);
            AddUnrecognizedValidator(CoreRunPathsOption);

            static void AddUnrecognizedValidator(Option option)
            {
                option.Validators.Add(result =>
                {
                    foreach (var token in result.Tokens.Where(t => t.Value.StartsWith("-", StringComparison.Ordinal)))
                        result.AddError($"Unrecognized option: {token.Value}");
                });
            }
        }
    }
}
