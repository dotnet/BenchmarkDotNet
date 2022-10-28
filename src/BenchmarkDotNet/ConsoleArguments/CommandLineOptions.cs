using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using CommandLine;
using CommandLine.Text;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.ConsoleArguments
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class CommandLineOptions
    {
        private const int DefaultDisassemblerRecursiveDepth = 1;
        private bool useDisassemblyDiagnoser;

        [Option('j', "job", Required = false, Default = "Default", HelpText = "Dry/Short/Medium/Long or Default")]
        public string BaseJob { get; set; }

        [Option('r', "runtimes", Required = false, HelpText = "Full target framework moniker for .NET Core and .NET. For Mono just 'Mono'. For NativeAOT please append target runtime version (example: 'nativeaot7.0'). First one will be marked as baseline!")]
        public IEnumerable<string> Runtimes { get; set; }

        [Option('e', "exporters", Required = false, HelpText = "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML")]
        public IEnumerable<string> Exporters { get; set; }

        [Option('m', "memory", Required = false, Default = false, HelpText = "Prints memory statistics")]
        public bool UseMemoryDiagnoser { get; set; }

        [Option('t', "threading", Required = false, Default = false, HelpText = "Prints threading statistics")]
        public bool UseThreadingDiagnoser { get; set; }

        [Option("exceptions", Required = false, Default = false, HelpText = "Prints exception statistics")]
        public bool UseExceptionDiagnoser { get; set; }

        [Option('d', "disasm", Required = false, Default = false, HelpText = "Gets disassembly of benchmarked code")]
        public bool UseDisassemblyDiagnoser
        {
            get => useDisassemblyDiagnoser || DisassemblerRecursiveDepth != DefaultDisassemblerRecursiveDepth || DisassemblerFilters.Any();
            set => useDisassemblyDiagnoser = value;
        }

        [Option('p', "profiler", Required = false, HelpText = "Profiles benchmarked code using selected profiler. Available options: EP/ETW/CV/NativeMemory")]
        public string Profiler { get; set; }

        [Option('f', "filter", Required = false, HelpText = "Glob patterns")]
        public IEnumerable<string> Filters { get; set; }

        [Option('h', "hide", Required = false, HelpText = "Hides columns by name")]
        public IEnumerable<string> HiddenColumns { get; set; }

        [Option('i', "inProcess", Required = false, Default = false, HelpText = "Run benchmarks in Process")]
        public bool RunInProcess { get; set; }

        [Option('a', "artifacts", Required = false, HelpText = "Valid path to accessible directory")]
        public DirectoryInfo ArtifactsDirectory { get; set; }

        [Option("outliers", Required = false, Default = OutlierMode.RemoveUpper, HelpText = "DontRemove/RemoveUpper/RemoveLower/RemoveAll")]
        public OutlierMode Outliers { get; set; }

        [Option("affinity", Required = false, HelpText = "Affinity mask to set for the benchmark process")]
        public int? Affinity { get; set; }

        [Option("allStats", Required = false, Default = false, HelpText = "Displays all statistics (min, max & more)")]
        public bool DisplayAllStatistics { get; set; }

        [Option("allCategories", Required = false, HelpText = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed")]
        public IEnumerable<string> AllCategories { get; set; }

        [Option("anyCategories", Required = false, HelpText = "Any Categories to run")]
        public IEnumerable<string> AnyCategories { get; set; }

        [Option("attribute", Required = false, HelpText = "Run all methods with given attribute (applied to class or method)")]
        public IEnumerable<string> AttributeNames { get; set; }

        [Option("join", Required = false, Default = false, HelpText = "Prints single table with results for all benchmarks")]
        public bool Join { get; set; }

        [Option("keepFiles", Required = false, Default = false, HelpText = "Determines if all auto-generated files should be kept or removed after running the benchmarks.")]
        public bool KeepBenchmarkFiles { get; set; }

        [Option("noOverwrite", Required = false, Default = false, HelpText = "Determines if the exported result files should not be overwritten (be default they are overwritten).")]
        public bool DontOverwriteResults { get; set; }

        [Option("counters", Required = false, HelpText = "Hardware Counters", Separator = '+')]
        public IEnumerable<string> HardwareCounters { get; set; }

        [Option("cli", Required = false, HelpText = "Path to dotnet cli (optional).")]
        public FileInfo CliPath { get; set; }

        [Option("packages", Required = false, HelpText = "The directory to restore packages to (optional).")]
        public DirectoryInfo RestorePath { get; set; }

        [Option("coreRun", Required = false, HelpText = "Path(s) to CoreRun (optional).")]
        public IReadOnlyList<FileInfo> CoreRunPaths { get; set; }

        [Option("monoPath", Required = false, HelpText = "Optional path to Mono which should be used for running benchmarks.")]
        public FileInfo MonoPath { get; set; }

        [Option("clrVersion", Required = false, HelpText = "Optional version of private CLR build used as the value of COMPLUS_Version env var.")]
        public string ClrVersion { get; set; }

        [Option("ilCompilerVersion", Required = false, HelpText = "Optional version of Microsoft.DotNet.ILCompiler which should be used to run with NativeAOT. Example: \"7.0.0-preview.3.22123.2\"")]
        public string ILCompilerVersion { get; set; }

        [Option("ilcPackages", Required = false, HelpText = @"Optional path to shipping packages produced by local dotnet/runtime build. Example: 'D:\projects\runtime\artifacts\packages\Release\Shipping\'")]
        public DirectoryInfo IlcPackages { get; set; }

        [Option("launchCount", Required = false, HelpText = "How many times we should launch process with target benchmark. The default is 1.")]
        public int? LaunchCount { get; set; }

        [Option("warmupCount", Required = false, HelpText = "How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.")]
        public int? WarmupIterationCount { get; set; }

        [Option("minWarmupCount", Required = false, HelpText = "Minimum count of warmup iterations that should be performed. The default is 6.")]
        public int? MinWarmupIterationCount { get; set; }

        [Option("maxWarmupCount", Required = false, HelpText = "Maximum count of warmup iterations that should be performed. The default is 50.")]
        public int? MaxWarmupIterationCount { get; set; }

        [Option("iterationTime", Required = false, HelpText = "Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default")]
        public int? IterationTimeInMilliseconds { get; set; }

        [Option("iterationCount", Required = false, HelpText = "How many target iterations should be performed. By default calculated by the heuristic.")]
        public int? IterationCount { get; set; }

        [Option("minIterationCount", Required = false, HelpText = "Minimum number of iterations to run. The default is 15.")]
        public int? MinIterationCount { get; set; }

        [Option("maxIterationCount", Required = false, HelpText = "Maximum number of iterations to run. The default is 100.")]
        public int? MaxIterationCount { get; set; }

        [Option("invocationCount", Required = false, HelpText = "Invocation count in a single iteration. By default calculated by the heuristic.")]
        public long? InvocationCount { get; set; }

        [Option("unrollFactor", Required = false, HelpText = "How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default")]
        public int? UnrollFactor { get; set; }

        [Option("strategy", Required = false, HelpText = "The RunStrategy that should be used. Throughput/ColdStart/Monitoring.")]
        public RunStrategy? RunStrategy { get; set; }

        [Option("platform", Required = false, HelpText = "The Platform that should be used. If not specified, the host process platform is used (default). AnyCpu/X86/X64/Arm/Arm64/LoongArch64.")]
        public Platform? Platform { get; set; }

        [Option("runOncePerIteration", Required = false, Default = false, HelpText = "Run the benchmark exactly once per iteration.")]
        public bool RunOncePerIteration { get; set; }

        [Option("info", Required = false, Default = false, HelpText = "Print environment information.")]
        public bool PrintInformation { get; set; }

        [Option("apples", Required = false, Default = false, HelpText = "Runs apples-to-apples comparison for specified Jobs.")]
        public bool ApplesToApples { get; set; }

        [Option("list", Required = false, Default = ListBenchmarkCaseMode.Disabled, HelpText = "Prints all of the available benchmark names. Flat/Tree")]
        public ListBenchmarkCaseMode ListBenchmarkCaseMode { get; set; }

        [Option("disasmDepth", Required = false, Default = DefaultDisassemblerRecursiveDepth, HelpText = "Sets the recursive depth for the disassembler.")]
        public int DisassemblerRecursiveDepth { get; set; }

        [Option("disasmFilter", Required = false, HelpText = "Glob patterns applied to full method signatures by the the disassembler.")]
        public IEnumerable<string> DisassemblerFilters { get; set; }

        [Option("disasmDiff", Required = false, Default = false, HelpText = "Generates diff reports for the disassembler.")]
        public bool DisassemblerDiff { get; set; }

        [Option("logBuildOutput", Required = false, HelpText = "Log Build output.")]
        public bool LogBuildOutput { get; set; }

        [Option("generateBinLog", Required = false, HelpText = "Generate msbuild binlog for builds")]
        public bool GenerateMSBuildBinLog { get; set; }

        [Option("buildTimeout", Required = false, HelpText = "Build timeout in seconds.")]
        public int? TimeOutInSeconds { get; set; }

        [Option("stopOnFirstError", Required = false, Default = false, HelpText = "Stop on first error.")]
        public bool StopOnFirstError { get; set; }

        [Option("statisticalTest", Required = false, HelpText = "Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s")]
        public string StatisticalTestThreshold { get; set; }

        [Option("disableLogFile", Required = false, HelpText = "Disables the logfile.")]
        public bool DisableLogFile { get; set; }

        [Option("maxWidth", Required = false, HelpText = "Max parameter column width, the default is 20.")]
        public int? MaxParameterColumnWidth { get; set; }

        [Option("envVars", Required = false, HelpText = "Colon separated environment variables (key:value)")]
        public IEnumerable<string> EnvironmentVariables { get; set; }

        [Option("memoryRandomization", Required = false, HelpText = "Specifies whether Engine should allocate some random-sized memory between iterations. It makes [GlobalCleanup] and [GlobalSetup] methods to be executed after every iteration.")]
        public bool MemoryRandomization { get; set; }

        [Option("wasmEngine", Required = false, HelpText = "Full path to a java script engine used to run the benchmarks, used by Wasm toolchain.")]
        public FileInfo WasmJavascriptEngine { get; set; }

        [Option("wasmArgs", Required = false, Default = "--expose_wasm", HelpText = "Arguments for the javascript engine used by Wasm toolchain.")]
        public string WasmJavaScriptEngineArguments { get; set; }

        [Option("customRuntimePack", Required = false, HelpText = "Path to a custom runtime pack. Only used for wasm/MonoAotLLVM currently.")]
        public string CustomRuntimePack { get; set; }

        [Option("AOTCompilerPath", Required = false, HelpText = "Path to Mono AOT compiler, used for MonoAotLLVM.")]
        public FileInfo AOTCompilerPath { get; set; }

        [Option("AOTCompilerMode", Required = false, Default = MonoAotCompilerMode.mini, HelpText = "Mono AOT compiler mode, either 'mini' or 'llvm'")]
        public MonoAotCompilerMode AOTCompilerMode { get; set; }

        [Option("wasmDataDir", Required = false, HelpText = "Wasm data directory")]
        public DirectoryInfo WasmDataDirectory { get; set; }

        [Option("noForcedGCs", Required = false, HelpText = "Specifying would not forcefully induce any GCs.")]
        public bool NoForcedGCs { get; set; }

        [Option("noOverheadEvaluation", Required = false, HelpText = "Specifying would not run the evaluation overhead iterations.")]
        public bool NoEvaluationOverhead { get; set; }

        [Option("resume", Required = false, Default = false, HelpText = "Continue the execution if the last run was stopped.")]
        public bool Resume { get; set; }

        internal bool UserProvidedFilters => Filters.Any() || AttributeNames.Any() || AllCategories.Any() || AnyCategories.Any();

        [Usage(ApplicationAlias = "")]
        [PublicAPI]
        public static IEnumerable<Example> Examples
        {
            get
            {
                var shortName = new UnParserSettings { PreferShortName = true };
                var longName = new UnParserSettings { PreferShortName = false };

                yield return new Example("Use Job.ShortRun for running the benchmarks", shortName, new CommandLineOptions { BaseJob = "short" });
                yield return new Example("Run benchmarks in process", shortName, new CommandLineOptions { RunInProcess = true });
                yield return new Example("Run benchmarks for .NET 4.7.2, .NET Core 2.1 and Mono. .NET 4.7.2 will be baseline because it was first.", longName, new CommandLineOptions { Runtimes = new[] { "net472", "netcoreapp2.1", "Mono" } });
                yield return new Example("Run benchmarks for .NET Core 2.0, .NET Core 2.1 and .NET Core 2.2. .NET Core 2.0 will be baseline because it was first.", longName, new CommandLineOptions { Runtimes = new[] { "netcoreapp2.0", "netcoreapp2.1", "netcoreapp2.2" } });
                yield return new Example("Use MemoryDiagnoser to get GC stats", shortName, new CommandLineOptions { UseMemoryDiagnoser = true });
                yield return new Example("Use DisassemblyDiagnoser to get disassembly", shortName, new CommandLineOptions { UseDisassemblyDiagnoser = true });
                yield return new Example("Use HardwareCountersDiagnoser to get hardware counter info", longName, new CommandLineOptions { HardwareCounters = new[] { nameof(HardwareCounter.CacheMisses), nameof(HardwareCounter.InstructionRetired) } });
                yield return new Example("Run all benchmarks exactly once", shortName, new CommandLineOptions { BaseJob = "Dry", Filters = new[] { Escape("*") } });
                yield return new Example("Run all benchmarks from System.Memory namespace", shortName, new CommandLineOptions { Filters = new[] { Escape("System.Memory*") } });
                yield return new Example("Run all benchmarks from ClassA and ClassB using type names", shortName, new CommandLineOptions { Filters = new[] { "ClassA", "ClassB" } });
                yield return new Example("Run all benchmarks from ClassA and ClassB using patterns", shortName, new CommandLineOptions { Filters = new[] { Escape("*.ClassA.*"), Escape("*.ClassB.*") } });
                yield return new Example("Run all benchmarks called `BenchmarkName` and show the results in single summary", longName, new CommandLineOptions { Join = true, Filters = new[] { Escape("*.BenchmarkName") } });
                yield return new Example("Run selected benchmarks once per iteration", longName, new CommandLineOptions { RunOncePerIteration = true });
                yield return new Example("Run selected benchmarks 100 times per iteration. Perform single warmup iteration and 5 actual workload iterations", longName, new CommandLineOptions { InvocationCount = 100, WarmupIterationCount = 1, IterationCount = 5});
                yield return new Example("Run selected benchmarks 250ms per iteration. Perform from 9 to 15 iterations", longName, new CommandLineOptions { IterationTimeInMilliseconds = 250, MinIterationCount = 9, MaxIterationCount = 15});
                yield return new Example("Run MannWhitney test with relative ratio of 5% for all benchmarks for .NET Core 2.0 (base) vs .NET Core 2.1 (diff). .NET Core 2.0 will be baseline because it was provided as first.", longName,
                    new CommandLineOptions { Filters = new[] { "*"}, Runtimes = new[] { "netcoreapp2.0", "netcoreapp2.1" }, StatisticalTestThreshold = "5%" });
                yield return new Example("Run benchmarks using environment variables 'ENV_VAR_KEY_1' with value 'value_1' and 'ENV_VAR_KEY_2' with value 'value_2'", longName,
                    new CommandLineOptions { EnvironmentVariables = new[] { "ENV_VAR_KEY_1:value_1", "ENV_VAR_KEY_2:value_2" } });
                yield return new Example("Hide Mean and Ratio columns (use double quotes for multi-word columns: \"Alloc Ratio\")", shortName, new CommandLineOptions { HiddenColumns = new[] { "Mean", "Ratio" }, });
            }
        }

        private static string Escape(string input) => UserInteractionHelper.EscapeCommandExample(input);
    }
}
