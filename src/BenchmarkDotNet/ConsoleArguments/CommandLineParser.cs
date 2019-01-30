using System.CodeDom;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal static class CommandLineParser
    {
        internal static int Parser(OptionHandler optionHandler, string[] args, ILogger logger, string description = null, Argument argument = null) 
        {
            var parser = new CommandLineBuilder(Root(optionHandler, description, argument))
                // parser
                .AddVersionOption()

                // middleware
                .UseHelp()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination()

                .Build();

            return parser.InvokeAsync(args, new CommandLineConsole(logger)).Result;

            
        }

        internal static RootCommand Root(OptionHandler optionHandler, string description, Argument argument = null)
        {
            var root = new RootCommand(argument: argument);
            root.Description = description;
            AddAllOptions(root);

            root.Handler = new MethodBinder(optionHandler.GetType().GetMethod("Init"), () => optionHandler);

            return root;
        }

        internal static void AddAllOptions(Command command)
        {
            command.AddOption(Job());
            command.AddOption(Runtimes());
            command.AddOption(Exporters());
            command.AddOption(UseMemoryDiagnoser());
            command.AddOption(Disasm());
            command.AddOption(Profiler());
            command.AddOption(Filter());
            command.AddOption(InProcess());
            command.AddOption(Artifacts());
            command.AddOption(Outliers());
            command.AddOption(Affinity());
            command.AddOption(AllStats());
            command.AddOption(AllCategories());
            command.AddOption(AnyCategories());
            command.AddOption(Attribute());
            command.AddOption(Join());
            command.AddOption(KeepFiles());
            command.AddOption(Counters());
            command.AddOption(Cli());
            command.AddOption(Packages());
            command.AddOption(CoreRun());
            command.AddOption(MonoPath());
            command.AddOption(ClrVersion());
            command.AddOption(CoreRtVersion());
            command.AddOption(IlcPath());
            command.AddOption(LaunchCount());
            command.AddOption(WarmupCount());
            command.AddOption(MinWarmupCount());
            command.AddOption(MaxWarmupCount());
            command.AddOption(IterationTime());
            command.AddOption(IterationCount());
            command.AddOption(MinIterationCount());
            command.AddOption(MaxIterationCount());
            command.AddOption(InvocationCount());
            command.AddOption(UnrollFactor());
            command.AddOption(RunOncePerIteration());
            command.AddOption(Info());
            command.AddOption(List());
            command.AddOption(DisasmDepth());
            command.AddOption(DisasmDiff());
            command.AddOption(BuildTimeout());
            command.AddOption(StopOnFirstError());
            command.AddOption(StatisticalTest());

            Option Job() => new Option(new[] { "-j", "--job" }, "Dry/Short/Medium/Long or Default",
                new Argument<string>("Default") { Arity = ArgumentArity.ZeroOrOne });

            Option Runtimes() => new Option(new[] { "-r", "--runtimes" },
                "Full target framework moniker for .NET Core and .NET. For Mono just 'Mono', for CoreRT just 'CoreRT'. First one will be marked as baseline!",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option Exporters() => new Option(new[] { "-e", "--exporters" },
                "GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option UseMemoryDiagnoser() => new Option(new[] { "-m", "--memory" },
                "Prints memory statistics",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option Disasm() => new Option(new[] { "-d", "--disasm" },
                "Gets disassembly of benchmarked code",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option Profiler() => new Option(new[] { "-p", "--profiler" },
                "Profiles benchmarked code using selected profiler. Currently the only available is \"ETW\" for Windows.",
                new Argument<string>() { Arity = ArgumentArity.ZeroOrOne });

            Option Filter() => new Option(new[] { "-f", "--filter" },
                "Glob patterns",
                new Argument<string[]>());

            Option InProcess() => new Option(new[] { "-i", "--inProcess" },
                "Run benchmarks in Process",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option Artifacts() => new Option(new[] { "-a", "--artifacts" },
                "Valid path to accessible directory",
                new Argument<DirectoryInfo>().ExistingOnly());

            Option Outliers() => new Option("--outliers",
                "None/OnlyUpper/OnlyLower/All",
                new Argument<OutlierMode>(OutlierMode.OnlyUpper) { Arity = ArgumentArity.ZeroOrOne });

            Option Affinity() => new Option("--affinity",
                "Affinity mask to set for the benchmark process",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option AllStats() => new Option("--allStats",
                "Displays all statistics (min, max & more)",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option AllCategories() => new Option(new[] { "--allCategories" },
                "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option AnyCategories() => new Option(new[] { "--anyCategories" },
                "Any Categories to run",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option Attribute() => new Option(new[] { "--attribute" },
                "Run all methods with given attribute (applied to class or method)",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option Join() => new Option("--join",
                "Prints single table with results for all benchmarks",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option KeepFiles() => new Option("--keepFiles",
                "Determines if all auto-generated files should be kept or removed after running the benchmarks.",
                new Argument<bool>(false) { Arity = ArgumentArity.ZeroOrOne });

            Option Counters() => new Option(new[] { "--counters" }, // TODO Separator = '+'
                "Hardware Counters",
                new Argument<string[]> { Arity = ArgumentArity.ZeroOrMore });

            Option Cli() => new Option(new[] { "--cli" },
                "Path to dotnet cli (optional).",
                new Argument<FileInfo> { Arity = ArgumentArity.ZeroOrOne }.ExistingOnly());

            Option Packages() => new Option(new[] { "--packages" },
                "The directory to restore packages to (optional).",
                new Argument<DirectoryInfo> { Arity = ArgumentArity.ZeroOrOne }.ExistingOnly());

            Option CoreRun() => new Option(new[] { "--coreRun" },
                "Path(s) to CoreRun (optional).",
                new Argument<FileInfo[]> { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly());

            Option MonoPath() => new Option("--monoPath",
                "Optional path to Mono which should be used for running benchmarks.",
                new Argument<FileInfo>() { Arity = ArgumentArity.ZeroOrOne });

            Option ClrVersion() => new Option("--clrVersion",
                "Optional version of private CLR build used as the value of COMPLUS_Version env var.",
                new Argument<string>() { Arity = ArgumentArity.ZeroOrOne });

            Option CoreRtVersion() => new Option("--coreRtVersion",
                "Optional version of Microsoft.DotNet.ILCompiler which should be used to run with CoreRT. Example: \"1.0.0-alpha-26414-01\"",
                new Argument<string>() { Arity = ArgumentArity.ZeroOrOne });

            Option IlcPath() => new Option("--ilcPath",
                "Optional IlcPath which should be used to run with private CoreRT build.",
                new Argument<DirectoryInfo>().ExistingOnly());

            Option LaunchCount() => new Option("--launchCount",
                "Affinity mask to set for the benchmark process",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option WarmupCount() => new Option("--warmupCount",
                "How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option MinWarmupCount() => new Option("--minWarmupCount",
                "Minimum count of warmup iterations that should be performed. The default is 6.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });


            Option MaxWarmupCount() => new Option("--maxWarmupCount",
                "Maximum count of warmup iterations that should be performed. The default is 50.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option IterationTime() => new Option("--iterationTime",
                "Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option IterationCount() => new Option("--iterationCount",
                "How many target iterations should be performed. By default calculated by the heuristic.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option MinIterationCount() => new Option("--minIterationCount",
                "Minimum number of iterations to run. The default is 15.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option MaxIterationCount() => new Option("--maxIterationCount",
                "Maximum number of iterations to run. The default is 100.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option InvocationCount() => new Option("--invocationCount",
                "Invocation count in a single iteration. By default calculated by the heuristic.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option UnrollFactor() => new Option("--unrollFactor",
                "How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option RunOncePerIteration() => new Option("--runOncePerIteration",
                "Run the benchmark exactly once per iteration.",
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            Option Info() => new Option("--info",
                "Run the benchmark exactly once per iteration.",
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            Option List() => new Option("--list",
                "Prints all of the available benchmark names. Flat/Tree",
                new Argument<ListBenchmarkCaseMode>(ListBenchmarkCaseMode.Disabled) { Arity = ArgumentArity.ZeroOrOne });

            Option DisasmDepth() => new Option("--disasmDepth",
                "Sets the recursive depth for the disassembler.",
                new Argument<int>(1) { Arity = ArgumentArity.ZeroOrOne });

            Option DisasmDiff() => new Option("--disasmDiff",
                "Generates diff reports for the disassembler.",
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            Option BuildTimeout() => new Option("--buildTimeout",
                "Build timeout in seconds.",
                new Argument<int?> { Arity = ArgumentArity.ZeroOrOne });

            Option StopOnFirstError() => new Option("--stopOnFirstError",
                "Stop on first error.",
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            Option StatisticalTest() => new Option("--statisticalTest",
                "Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s",
                new Argument<string> { Arity = ArgumentArity.ZeroOrOne });
        }
    }
}
