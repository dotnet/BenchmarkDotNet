using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Mathematics;
using CommandLine;

namespace BenchmarkDotNet.ConsoleArguments
{
    public class CommandLineOptions
    {
        [Option('j', "job", Required = false, Default = "Default", HelpText = "Dry/Short/Medium/Long or Default")]
        public string BaseJob { get; set; }

        [Option('r', "runtimes", Required = false, HelpText = "Clr/Core/Mono/CoreRt")]
        public IEnumerable<string> Runtimes { get; set; }
        
        [Option('e', "exporters", Required = false, HelpText = "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML")]
        public IEnumerable<string> Exporters { get; set; }
        
        [Option('m', "memory", Required = false, Default = false, HelpText = "Prints memory statistics.")]
        public bool UseMemoryDiagnoser { get; set; }

        [Option('d', "disassm", Required = false, Default = false, HelpText = "Gets diassembly for benchmarked code")]
        public bool UseDisassemblyDiagnoser { get; set; }
        
        [Option('f', "filter", Required = false, HelpText = "Glob patterns")]
        public IEnumerable<string> Filters { get; set; }

        [Option("outliers", Required = false, Default = OutlierMode.OnlyUpper, HelpText = "None/OnlyUpper/OnlyLower/All")]
        public OutlierMode Outliers { get; set; }
        
        [Option("affinity", Required = false, HelpText = "Affinity mask to set for the benchmark process")]
        public int? Affinity { get; set; }
        
        [Option("allStats", Required = false, Default = false, HelpText = "Displays all statistics (min, max & more)")]
        public bool DisplayAllStatistics { get; set; }

        [Option("inProcess", Required = false, Default = false, HelpText = "Run benchmarks in Process")]
        public bool RunInProcess { get; set; }

        [Option("artifacts", Required = false, HelpText = "Any valid path to accessible directory")]
        public DirectoryInfo ArtifactsDirectory{ get; set; }
        
        [Option("categories", Required = false, HelpText = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed")]
        public IEnumerable<string> AllCategories { get; set; }
        
        [Option("anyCategories", Required = false, HelpText = "Any Categories to run")]
        public IEnumerable<string> AnyCategories { get; set; }
        
        [Option("attribute", Required = false, HelpText = "Run all methods with given attribute (applied to class or method)")]
        public IEnumerable<string> AttributeNames { get; set; }
        
        [Option("join", Required = false, Default = false, HelpText = "Prints single table with results for all benchmarks")]
        public bool Join { get; set; }
    }
}