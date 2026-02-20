using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.CommandLine;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using Perfolizer.Mathematics.OutlierDetection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;

namespace BenchmarkDotNet.ConsoleArguments
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class CommandLineOptions
    {
        private const int DefaultDisassemblerRecursiveDepth = 1;
        private bool useDisassemblyDiagnoser;

        // Options defined in the same order as the original CommandLineParser implementation
        public static readonly Option<string> BaseJobOption = new("--job", "-j")
        { Description = "Dry/Short/Medium/Long or Default", DefaultValueFactory = _ => "Default" };

        public static readonly Option<string[]> RuntimesOption = new("--runtimes", "-r")
        { Description = "Full target framework moniker for .NET Core and .NET. For Mono just 'Mono'. For NativeAOT please append target runtime version (example: 'nativeaot7.0'). First one will be marked as baseline!" };

        public static readonly Option<string[]> ExportersOption = new("--exporters", "-e")
        { Description = "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML" };

        public static readonly Option<bool> MemoryOption = new("--memory", "-m")
        { Description = "Prints memory statistics" };

        public static readonly Option<bool> ThreadingOption = new("--threading", "-t")
        { Description = "Prints threading statistics" };

        public static readonly Option<bool> ExceptionsOption = new("--exceptions")
        { Description = "Prints exception statistics" };

        public static readonly Option<bool> DisassemblyOption = new("--disasm", "-d")
        { Description = "Gets disassembly of benchmarked code" };

        public static readonly Option<string> ProfilerOption = new("--profiler", "-p")
        { Description = "Profiles benchmarked code using selected profiler. Available options: EP/ETW/CV/NativeMemory" };

        public static readonly Option<string[]> FiltersOption = new("--filter", "-f")
        { Description = "Glob patterns" };

        public static readonly Option<string[]> HiddenColumnsOption = new("--hide", "-h")
        { Description = "Hides columns by name" };

        public static readonly Option<bool> RunInProcessOption = new("--inProcess", "-i")
        { Description = "Run benchmarks in Process" };

        public static readonly Option<DirectoryInfo> ArtifactsDirectoryOption = new("--artifacts", "-a")
        { Description = "Valid path to accessible directory" };

        public static readonly Option<OutlierMode> OutliersOption = new("--outliers")
        { Description = "DontRemove/RemoveUpper/RemoveLower/RemoveAll", DefaultValueFactory = _ => OutlierMode.RemoveUpper };

        public static readonly Option<int?> AffinityOption = new("--affinity")
        { Description = "Affinity mask to set for the benchmark process" };

        public static readonly Option<bool> DisplayAllStatisticsOption = new("--allStats")
        { Description = "Displays all statistics (min, max & more)" };

        public static readonly Option<string[]> AllCategoriesOption = new("--allCategories")
        { Description = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed" };

        public static readonly Option<string[]> AnyCategoriesOption = new("--anyCategories")
        { Description = "Any Categories to run" };

        public static readonly Option<string[]> AttributeNamesOption = new("--attribute")
        { Description = "Run all methods with given attribute (applied to class or method)" };

        public static readonly Option<bool> JoinOption = new("--join")
        { Description = "Prints single table with results for all benchmarks" };

        public static readonly Option<bool> KeepBenchmarkFilesOption = new("--keepFiles")
        { Description = "Determines if all auto-generated files should be kept or removed after running the benchmarks." };

        public static readonly Option<bool> DontOverwriteResultsOption = new("--noOverwrite")
        { Description = "Determines if the exported result files should not be overwritten (be default they are overwritten)." };

        public static readonly Option<string[]> HardwareCountersOption = new("--counters")
        { Description = "Hardware Counters" };

        public static readonly Option<FileInfo> CliPathOption = new("--cli")
        { Description = "Path to dotnet cli (optional)." };

        public static readonly Option<FileInfo> RestorePathOption = new("--packages")
        { Description = "The directory to restore packages to (optional)." };

        public static readonly Option<FileInfo[]> CoreRunPathsOption = new("--coreRun")
        { Description = "Path(s) to CoreRun (optional)." };

        public static readonly Option<FileInfo> MonoPathOption = new("--monoPath")
        { Description = "Optional path to Mono which should be used for running benchmarks." };

        public static readonly Option<string> ClrVersionOption = new("--clrVersion")
        { Description = "Optional version of private CLR build used as the value of COMPLUS_Version env var." };

        public static readonly Option<string> ILCompilerVersionOption = new("--ilCompilerVersion")
        { Description = "Optional version of Microsoft.DotNet.ILCompiler which should be used to run with NativeAOT. Example: \"7.0.0-preview.3.22123.2\"" };

        public static readonly Option<DirectoryInfo> IlcPackagesOption = new("--ilcPackages")
        { Description = "Optional path to shipping packages produced by local dotnet/runtime build." };

        public static readonly Option<int?> LaunchCountOption = new("--launchCount")
        { Description = "How many times we should launch process with target benchmark. The default is 1." };

        public static readonly Option<int?> WarmupCountOption = new("--warmupCount")
        { Description = "How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic." };

        public static readonly Option<int?> MinWarmupCountOption = new("--minWarmupCount")
        { Description = "Minimum count of warmup iterations that should be performed. The default is 6." };

        public static readonly Option<int?> MaxWarmupCountOption = new("--maxWarmupCount")
        { Description = "Maximum count of warmup iterations that should be performed. The default is 50." };

        public static readonly Option<int?> IterationTimeOption = new("--iterationTime")
        { Description = "Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default" };

        public static readonly Option<int?> IterationCountOption = new("--iterationCount")
        { Description = "How many target iterations should be performed. By default calculated by the heuristic." };

        public static readonly Option<int?> MinIterationCountOption = new("--minIterationCount")
        { Description = "Minimum number of iterations to run. The default is 15." };

        public static readonly Option<int?> MaxIterationCountOption = new("--maxIterationCount")
        { Description = "Maximum number of iterations to run. The default is 100." };

        public static readonly Option<long?> InvocationCountOption = new("--invocationCount")
        { Description = "Invocation count in a single iteration. By default calculated by the heuristic." };

        public static readonly Option<int?> UnrollFactorOption = new("--unrollFactor")
        { Description = "How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default" };

        public static readonly Option<RunStrategy?> RunStrategyOption = new("--strategy")
        { Description = "The RunStrategy that should be used. Throughput/ColdStart/Monitoring." };

        public static readonly Option<Platform?> PlatformOption = new("--platform")
        { Description = "The Platform that should be used. If not specified, the host process platform is used (default). AnyCpu/X86/X64/Arm/Arm64/LoongArch64." };

        public static readonly Option<bool> RunOnceOption = new("--runOncePerIteration")
        { Description = "Run the benchmark exactly once per iteration." };

        public static readonly Option<bool> PrintInformationOption = new("--info")
        { Description = "Print environment information." };

        public static readonly Option<bool> ApplesToApplesOption = new("--apples")
        { Description = "Runs apples-to-apples comparison for specified Jobs." };

        public static readonly Option<ListBenchmarkCaseMode> ListBenchmarkCaseModeOption = new("--list")
        { Description = "Prints all of the available benchmark names. Flat/Tree", DefaultValueFactory = _ => ListBenchmarkCaseMode.Disabled };

        public static readonly Option<int> DisassemblerDepthOption = new("--disasmDepth")
        { Description = "Sets the recursive depth for the disassembler.", DefaultValueFactory = _ => DefaultDisassemblerRecursiveDepth };

        public static readonly Option<string[]> DisassemblerFiltersOption = new("--disasmFilter")
        { Description = "Glob patterns applied to full method signatures by the disassembler." };

        public static readonly Option<bool> DisassemblerDiffOption = new("--disasmDiff")
        { Description = "Generates diff reports for the disassembler." };

        public static readonly Option<bool> LogBuildOutputOption = new("--logBuildOutput")
        { Description = "Log Build output." };

        public static readonly Option<bool> GenerateBinLogOption = new("--generateBinLog")
        { Description = "Generate msbuild binlog for builds" };

        public static readonly Option<int?> TimeoutOption = new("--buildTimeout")
        { Description = "Build timeout in seconds." };

        public static readonly Option<WakeLockType?> WakeLockOption = new("--wakeLock")
        { Description = "Prevents the system from entering sleep or turning off the display. None/System/Display." };

        public static readonly Option<bool> StopOnFirstErrorOption = new("--stopOnFirstError")
        { Description = "Stop on first error." };

        public static readonly Option<string> StatisticalTestThresholdOption = new("--statisticalTest")
        { Description = "Threshold for Mann-Whitney U Test. Examples: 5%, 10ms, 100ns, 1s" };

        public static readonly Option<bool> DisableLogFileOption = new("--disableLogFile")
        { Description = "Disables the logfile." };

        public static readonly Option<int?> MaxParameterColumnWidthOption = new("--maxWidth")
        { Description = "Max parameter column width, the default is 20." };

        public static readonly Option<string[]> EnvironmentVariablesOption = new("--envVars")
        { Description = "Colon separated environment variables (key:value)" };

        public static readonly Option<bool> MemoryRandomizationOption = new("--memoryRandomization")
        { Description = "Specifies whether Engine should allocate some random-sized memory between iterations." };

        public static readonly Option<FileInfo> WasmJavascriptEngineOption = new("--wasmEngine")
        { Description = "Full path to a java script engine used to run the benchmarks, used by Wasm toolchain." };

        public static readonly Option<string> WasmJavaScriptEngineArgumentsOption = new("--wasmArgs")
        { Description = "Arguments for the javascript engine used by Wasm toolchain.", DefaultValueFactory = _ => "--expose_wasm" };

        public static readonly Option<string> CustomRuntimePackOption = new("--customRuntimePack")
        { Description = "Path to a custom runtime pack. Only used for wasm/MonoAotLLVM currently." };

        public static readonly Option<FileInfo> AOTCompilerPathOption = new("--AOTCompilerPath")
        { Description = "Path to Mono AOT compiler, used for MonoAotLLVM." };

        public static readonly Option<MonoAotCompilerMode> AOTCompilerModeOption = new("--AOTCompilerMode")
        { Description = "Mono AOT compiler mode, either 'mini' or 'llvm'", DefaultValueFactory = _ => MonoAotCompilerMode.mini };

        public static readonly Option<DirectoryInfo> WasmDataDirectoryOption = new("--wasmDataDir")
        { Description = "Wasm data directory" };

        public static readonly Option<bool> WasmCoreCLROption = new("--wasmCoreCLR")
        { Description = "Use CoreCLR runtime pack instead of the Mono runtime pack for WASM benchmarks." };

        public static readonly Option<bool> NoForcedGCsOption = new("--noForcedGCs")
        { Description = "Specifying would not forcefully induce any GCs." };

        public static readonly Option<bool> NoEvaluationOverheadOption = new("--noOverheadEvaluation")
        { Description = "Specifying would not run the evaluation overhead iterations." };

        public static readonly Option<bool> ResumeOption = new("--resume")
        { Description = "Continue the execution if the last run was stopped." };

        // Properties
        public string BaseJob { get; set; } = "";
        public IEnumerable<string> Runtimes { get; set; } = [];
        public IEnumerable<string> Exporters { get; set; } = [];
        public bool UseMemoryDiagnoser { get; set; }
        public bool UseThreadingDiagnoser { get; set; }
        public bool UseExceptionDiagnoser { get; set; }
        public bool UseDisassemblyDiagnoser
        {
            get => useDisassemblyDiagnoser || DisassemblerRecursiveDepth != DefaultDisassemblerRecursiveDepth || DisassemblerFilters.Any();
            set => useDisassemblyDiagnoser = value;
        }
        public string? Profiler { get; set; }
        public IEnumerable<string> Filters { get; set; } = [];
        public IEnumerable<string> HiddenColumns { get; set; } = [];
        public bool RunInProcess { get; set; }
        public DirectoryInfo? ArtifactsDirectory { get; set; }
        public OutlierMode Outliers { get; set; }
        public int? Affinity { get; set; }
        public bool DisplayAllStatistics { get; set; }
        public IEnumerable<string> AllCategories { get; set; } = [];
        public IEnumerable<string> AnyCategories { get; set; } = [];
        public IEnumerable<string> AttributeNames { get; set; } = [];
        public bool Join { get; set; }
        public bool KeepBenchmarkFiles { get; set; }
        public bool DontOverwriteResults { get; set; }
        public IEnumerable<string> HardwareCounters { get; set; } = [];
        public FileInfo? CliPath { get; set; }
        public DirectoryInfo? RestorePath { get; set; }
        public IReadOnlyList<FileInfo> CoreRunPaths { get; set; } = [];
        public FileInfo? MonoPath { get; set; }
        public string? ClrVersion { get; set; }
        public string? ILCompilerVersion { get; set; }
        public DirectoryInfo? IlcPackages { get; set; }
        public int? LaunchCount { get; set; }
        public int? WarmupIterationCount { get; set; }
        public int? MinWarmupIterationCount { get; set; }
        public int? MaxWarmupIterationCount { get; set; }
        public int? IterationTimeInMilliseconds { get; set; }
        public int? IterationCount { get; set; }
        public int? MinIterationCount { get; set; }
        public int? MaxIterationCount { get; set; }
        public long? InvocationCount { get; set; }
        public int? UnrollFactor { get; set; }
        public RunStrategy? RunStrategy { get; set; }
        public Platform? Platform { get; set; }
        public bool RunOncePerIteration { get; set; }
        public bool PrintInformation { get; set; }
        public bool ApplesToApples { get; set; }
        public ListBenchmarkCaseMode ListBenchmarkCaseMode { get; set; }
        public int DisassemblerRecursiveDepth { get; set; }
        public IEnumerable<string> DisassemblerFilters { get; set; } = [];
        public bool DisassemblerDiff { get; set; }
        public bool LogBuildOutput { get; set; }
        public bool GenerateMSBuildBinLog { get; set; }
        public int? TimeOutInSeconds { get; set; }
        public WakeLockType? WakeLock { get; set; }
        public bool StopOnFirstError { get; set; }
        public string? StatisticalTestThreshold { get; set; }
        public bool DisableLogFile { get; set; }
        public int? MaxParameterColumnWidth { get; set; }
        public IEnumerable<string> EnvironmentVariables { get; set; } = [];
        public bool MemoryRandomization { get; set; }
        public FileInfo? WasmJavascriptEngine { get; set; }
        public string? WasmJavaScriptEngineArguments { get; set; }
        public string? CustomRuntimePack { get; set; }
        public FileInfo? AOTCompilerPath { get; set; }
        public MonoAotCompilerMode AOTCompilerMode { get; set; }
        public DirectoryInfo? WasmDataDirectory { get; set; }
        public bool WasmCoreCLR { get; set; }
        public bool NoForcedGCs { get; set; }
        public bool NoEvaluationOverhead { get; set; }
        public bool Resume { get; set; }
        internal bool UserProvidedFilters => Filters.Any() || AttributeNames.Any() || AllCategories.Any() || AnyCategories.Any();

        private static string Escape(string input) => UserInteractionHelper.EscapeCommandExample(input);
    }
}