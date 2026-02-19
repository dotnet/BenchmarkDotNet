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
        public static readonly Option<string> BaseJobOption = new("--job", "-j")
        { Description = "Dry/Short/Medium/Long or Default", DefaultValueFactory = _ => "Default" };

        public static readonly Option<string[]> RuntimesOption = new("--runtimes", "-r")
        { Description = "Target framework monikers" };

        public static readonly Option<string[]> ExportersOption = new("--exporters", "-e")
        { Description = "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML" };

        public static readonly Option<string[]> FiltersOption = new("--filter", "-f")
        { Description = "Glob patterns" };

        public static readonly Option<bool> MemoryOption = new("--memory", "-m")
        { Description = "Prints memory statistics" };

        public static readonly Option<DirectoryInfo> ArtifactsDirectoryOption = new("--artifacts", "-a")
        { Description = "Path to artifacts directory" };

        public static readonly Option<bool> RunInProcessOption = new("--inProcess", "-i")
        { Description = "Run in Process" };

        public static readonly Option<bool> ThreadingOption = new("--threading")
        { Description = "Prints threading statistics" };

        public static readonly Option<bool> ExceptionsOption = new("--exceptions")
        { Description = "Prints exceptions statistics" };

        public static readonly Option<bool> DisassemblyOption = new("--disassembly")
        { Description = "Prints disassembly" };

        public static readonly Option<int> DisassemblerDepthOption = new("--disassemblerDepth")
        { Description = "Disassembler recursive depth", DefaultValueFactory = _ => 1 };

        public static readonly Option<string[]> DisassemblerFiltersOption = new("--disassemblerFilters")
        { Description = "Disassembler filters" };

        public static readonly Option<bool> DisassemblerDiffOption = new("--disassemblerDiff")
        { Description = "Disassembler diff" };

        public static readonly Option<string> ProfilerOption = new("--profiler")
        { Description = "Profiler (ETW, EventPipe, etc)" };

        public static readonly Option<bool> DisplayAllStatisticsOption = new("--allStats")
        { Description = "Displays all statistics" };

        public static readonly Option<string> StatisticalTestThresholdOption = new("--statisticalTest")
        { Description = "Statistical test threshold" };

        public static readonly Option<FileInfo[]> CoreRunPathsOption = new("--coreRun")
        { Description = "Paths to CoreRun" };

        public static readonly Option<FileInfo> CliPathOption = new("--cli")
        { Description = "Path to dotnet cli" };

        public static readonly Option<FileInfo> RestorePathOption = new("--restore")
        { Description = "Path to restore packages" };

        public static readonly Option<string> ClrVersionOption = new("--clrVersion")
        { Description = "CLR version" };

        public static readonly Option<bool> JoinOption = new("--join")
        { Description = "Join summary" };

        public static readonly Option<bool> KeepBenchmarkFilesOption = new("--keepFiles")
        { Description = "Keep benchmark files" };

        public static readonly Option<bool> DontOverwriteResultsOption = new("--dontOverwrite")
        { Description = "Don't overwrite results" };

        public static readonly Option<bool> StopOnFirstErrorOption = new("--stopOnFirstError")
        { Description = "Stop on first error" };

        public static readonly Option<bool> DisableLogFileOption = new("--disableLogFile")
        { Description = "Disable log file" };

        public static readonly Option<bool> LogBuildOutputOption = new("--logBuildOutput")
        { Description = "Log build output" };

        public static readonly Option<bool> GenerateBinLogOption = new("--buildLog")
        { Description = "Generate MSBuild bin log" };

        public static readonly Option<bool> ApplesToApplesOption = new("--applesToApples")
        { Description = "Apples to Apples" };

        public static readonly Option<bool> ResumeOption = new("--resume")
        { Description = "Resume" };

        public static readonly Option<int?> TimeoutOption = new("--timeout")
        { Description = "Timeout in seconds" };

        public static readonly Option<int?> LaunchCountOption = new("--launchCount")
        { Description = "Launch count" };

        public static readonly Option<int?> WarmupCountOption = new("--warmupCount")
        { Description = "Warmup iteration count" };

        public static readonly Option<int?> IterationCountOption = new("--iterationCount")
        { Description = "Iteration count" };

        public static readonly Option<long?> InvocationCountOption = new("--invocationCount")
        { Description = "Invocation count" };

        public static readonly Option<int?> UnrollFactorOption = new("--unrollFactor")
        { Description = "Unroll factor" };

        public static readonly Option<bool> RunOnceOption = new("--runOnce")
        { Description = "Run once per iteration" };

        public static readonly Option<bool> MemoryRandomizationOption = new("--memoryRandomization")
        { Description = "Memory randomization" };

        public static readonly Option<bool> NoForcedGCsOption = new("--noForcedGCs")
        { Description = "No forced GCs" };

        public static readonly Option<string[]> AllCategoriesOption = new("--allCategories")
        { Description = "All Categories" };

        public static readonly Option<string[]> AnyCategoriesOption = new("--anyCategories")
        { Description = "Any Categories" };

        public static readonly Option<string[]> AttributeNamesOption = new("--attributeNames")
        { Description = "Attribute Names" };

        public static readonly Option<string[]> HiddenColumnsOption = new("--hiddenColumns")
        { Description = "Hidden Columns" };

        private const int DefaultDisassemblerRecursiveDepth = 1;
        private bool useDisassemblyDiagnoser;
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
