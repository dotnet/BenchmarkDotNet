using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.ConsoleArguments
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class CommandLineOptions
    {
        public string BaseJob { get; set; }

        public IEnumerable<string> Runtimes { get; set; }

        public IEnumerable<string> Exporters { get; set; }

        public bool UseMemoryDiagnoser { get; set; }

        public bool UseDisassemblyDiagnoser { get; set; }

        public string Profiler { get; set; }

        public IEnumerable<string> Filters { get; set; }

        public bool RunInProcess { get; set; }

        public DirectoryInfo ArtifactsDirectory { get; set; }

        public OutlierMode Outliers { get; set; }

        public int? Affinity { get; set; }

        public bool DisplayAllStatistics { get; set; }

        public IEnumerable<string> AllCategories { get; set; }

        public IEnumerable<string> AnyCategories { get; set; }

        public IEnumerable<string> AttributeNames { get; set; }

        public bool Join { get; set; }

        public bool KeepBenchmarkFiles { get; set; }

        public IEnumerable<string> HardwareCounters { get; set; }

        public FileInfo CliPath { get; set; }

        public DirectoryInfo RestorePath { get; set; }

        public IReadOnlyList<FileInfo> CoreRunPaths { get; set; }

        public FileInfo MonoPath { get; set; }

        public string ClrVersion { get; set; }

        public string CoreRtVersion { get; set; }

        public DirectoryInfo CoreRtPath { get; set; }

        public int? LaunchCount { get; set; }

        public int? WarmupIterationCount { get; set; }

        public int? MinWarmupIterationCount { get; set; }

        public int? MaxWarmupIterationCount { get; set; }

        public int? IterationTimeInMilliseconds { get; set; }

        public int? IterationCount { get; set; }

        public int? MinIterationCount { get; set; }

        public int? MaxIterationCount { get; set; }

        public int? InvocationCount { get; set; }

        public int? UnrollFactor { get; set; }

        public bool RunOncePerIteration { get; set; }

        public bool PrintInformation { get; set; }

        public ListBenchmarkCaseMode ListBenchmarkCaseMode { get; set; }

        public int DisassemblerRecursiveDepth { get; set; }

        public bool DisassemblerDiff { get; set; }

        public int? TimeOutInSeconds { get; set; }

        public bool StopOnFirstError { get; set; }

        public string StatisticalTestThreshold { get; set; }

        internal bool UserProvidedFilters => Filters.Any() || AttributeNames.Any() || AllCategories.Any() || AnyCategories.Any(); 

//        [Usage(ApplicationAlias = "")]
//        [PublicAPI]
//        public static IEnumerable<Example> Examples
//        {
//            get
//            {
//                var shortName = new UnParserSettings { PreferShortName = true };
//                var longName = new UnParserSettings { PreferShortName = false };
//
//                yield return new Example("Use Job.ShortRun for running the benchmarks", shortName, new CommandLineOptions { BaseJob = "short" });
//                yield return new Example("Run benchmarks in process", shortName, new CommandLineOptions { RunInProcess = true });
//                yield return new Example("Run benchmarks for .NET 4.7.2, .NET Core 2.1 and Mono. .NET 4.7.2 will be baseline because it was first.", longName, new CommandLineOptions { Runtimes = new[] { "net472", "netcoreapp2.1", "Mono" } });
//                yield return new Example("Run benchmarks for .NET Core 2.0, .NET Core 2.1 and .NET Core 2.2. .NET Core 2.0 will be baseline because it was first.", longName, new CommandLineOptions { Runtimes = new[] { "netcoreapp2.0", "netcoreapp2.1", "netcoreapp2.2" } });
//                yield return new Example("Use MemoryDiagnoser to get GC stats", shortName, new CommandLineOptions { UseMemoryDiagnoser = true });
//                yield return new Example("Use DisassemblyDiagnoser to get disassembly", shortName, new CommandLineOptions { UseDisassemblyDiagnoser = true });
//                yield return new Example("Use HardwareCountersDiagnoser to get hardware counter info", longName, new CommandLineOptions { HardwareCounters = new [] { nameof(HardwareCounter.CacheMisses), nameof(HardwareCounter.InstructionRetired) } });
//                yield return new Example("Run all benchmarks exactly once", shortName, new CommandLineOptions { BaseJob = "Dry", Filters = new[] { HandleWildcardsOnUnix("*") } });
//                yield return new Example("Run all benchmarks from System.Memory namespace", shortName, new CommandLineOptions { Filters = new[] { HandleWildcardsOnUnix("System.Memory*") } });
//                yield return new Example("Run all benchmarks from ClassA and ClassB using type names", shortName, new CommandLineOptions { Filters = new[] { "ClassA", "ClassB" } });
//                yield return new Example("Run all benchmarks from ClassA and ClassB using patterns", shortName, new CommandLineOptions { Filters = new[] { HandleWildcardsOnUnix("*.ClassA.*"), HandleWildcardsOnUnix("*.ClassB.*") } });
//                yield return new Example("Run all benchmarks called `BenchmarkName` and show the results in single summary", longName, new CommandLineOptions { Join = true, Filters = new[] { HandleWildcardsOnUnix("*.BenchmarkName") } });
//                yield return new Example("Run selected benchmarks once per iteration", longName, new CommandLineOptions { RunOncePerIteration = true });
//                yield return new Example("Run selected benchmarks 100 times per iteration. Perform single warmup iteration and 5 actual workload iterations", longName, new CommandLineOptions { InvocationCount = 100, WarmupIterationCount = 1, IterationCount = 5});
//                yield return new Example("Run selected benchmarks 250ms per iteration. Perform from 9 to 15 iterations", longName, new CommandLineOptions { IterationTimeInMilliseconds = 250, MinIterationCount = 9, MaxIterationCount = 15});
//                yield return new Example("Run MannWhitney test with relative ratio of 5% for all benchmarks for .NET Core 2.0 (base) vs .NET Core 2.1 (diff). .NET Core 2.0 will be baseline because it was provided as first.", longName, 
//                    new CommandLineOptions { Filters = new [] { "*"}, Runtimes = new[] { "netcoreapp2.0", "netcoreapp2.1" }, StatisticalTestThreshold = "5%" });
//            }
//        }

//        private static string HandleWildcardsOnUnix(string input) => !RuntimeInformation.IsWindows() && input.IndexOf('*') >= 0 ? $"'{input}'" : input; // #842
    }
}