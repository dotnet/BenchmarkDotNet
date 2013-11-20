using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private readonly BenchmarkCompetition[] competitions = new BenchmarkCompetition[]
            {
                new ArrayIterationCompetition(),
                new ForeachArrayCompetition(), 
                new ForeachListCompetition(), 
                new IncrementCompetition(),
                new MakeRefVsBoxingCompetition(), 
                new MultidimensionalArrayCompetition(),                
                new ReverseSortCompetition(),
                new ShiftVsMultiplyCompetition(), 
                new StackFrameCompetition(),
                new CacheConsciousBinarySearchCompetition()
            };

        private string outputFileName;


        private void Run(string[] args)
        {
            Array.Sort(competitions, (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
            args = ReadArgumentList(args);
            ApplyBenchmarkSettings(args);
            RunCompetitions(args);
        }

        private string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintHelp();
                ConsoleHelper.WriteLineHelp("Argument list is empty. Please, print the argument list:");
                args = ConsoleHelper.ReadArgsLine();
                ConsoleHelper.NewLine();
            }
            return args;
        }

        private void ApplyBenchmarkSettings(string[] args)
        {
            BenchmarkSettings.Instance.DetailedMode = Contains(args, "-d", "--details");

            if (Contains(args, "-dw", "--disable-warmup"))
                BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount = 0;

            if (Contains(args, "-s", "--single"))
            {
                BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount = 0;
                BenchmarkSettings.Instance.DefaultResultIterationCount = 1;
            }

            outputFileName = GetStringArgValue(args, "-of", "--output-file");

            int? resultIterationCount = GetInt32ArgValue(args, "-rc", "--result-count");
            if (resultIterationCount != null)
                BenchmarkSettings.Instance.DefaultResultIterationCount = resultIterationCount.Value;

            int? warmUpIterationCount = GetInt32ArgValue(args, "-wc", "--warmup-count");
            if (warmUpIterationCount != null)
                BenchmarkSettings.Instance.DefaultWarmUpIterationCount = warmUpIterationCount.Value;

            int? maxWarmUpIterationCount = GetInt32ArgValue(args, "-mwc", "--max-warmup-count");
            if (maxWarmUpIterationCount != null)
                BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount = maxWarmUpIterationCount.Value;

            int? maxWarpUpError = GetInt32ArgValue(args, "-mwe", "--max-warmup-error");
            if (maxWarpUpError != null)
                BenchmarkSettings.Instance.DefaultMaxWarmUpError = maxWarpUpError.Value / 100.0;

            bool? printBenchmark = GetBoolArgValue(args, "-pb", "--print-benchmark");
            if (printBenchmark != null)
                BenchmarkSettings.Instance.DefaultPrintBenchmarkBodyToConsole = printBenchmark.Value;

            int? processorAffinity = GetInt32ArgValue(args, "-pa", "--processor-affinity");
            if (processorAffinity != null)
                BenchmarkSettings.Instance.DefaultProcessorAffinity = processorAffinity.Value;
        }

        private void RunCompetitions(string[] args)
        {
            bool runAll = Contains(args, "-a", "--all");

            for (int i = 0; i < competitions.Length; i++)
            {
                var competition = competitions[i];
                if (runAll || args.Any(arg => competition.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i))
                {
                    ConsoleHelper.WriteLineHeader("Target program: " + competition.Name);
                    competition.Run();
                    SaveCompetitionResults(competition);
                    ConsoleHelper.NewLine();
                }
            }
        }

        private void SaveCompetitionResults(BenchmarkCompetition competition)
        {
            if (!string.IsNullOrEmpty(outputFileName))
                using (var writer = new StreamWriter(outputFileName))
                {
                    ConsoleHelper.SetOut(writer);
                    competition.PrintResults();
                    ConsoleHelper.RestoreDefaultOut();
                }
        }

        private void PrintHelp()
        {
            ConsoleHelper.WriteLineHelp("Usage: Benchmarks <competitions-names> [<arguments>]");
            ConsoleHelper.WriteLineHelp("Arguments:");
            ConsoleHelper.WriteLineHelp("  -a, --all");
            ConsoleHelper.WriteLineHelp("      Run all available competitions");
            ConsoleHelper.WriteLineHelp("  -d, --details");
            ConsoleHelper.WriteLineHelp("      Show detailed results");
            ConsoleHelper.WriteLineHelp("  -rc=<n>, --result-count=<n>");
            ConsoleHelper.WriteLineHelp("      Result set iteration count");
            ConsoleHelper.WriteLineHelp("  -wc=<n>, --warmup-count=<n>");
            ConsoleHelper.WriteLineHelp("      WarmUp set default iteration count");
            ConsoleHelper.WriteLineHelp("  -mwc=<n>, --max-warmup-count=<n>");
            ConsoleHelper.WriteLineHelp("      WarmUp set max iteration count");
            ConsoleHelper.WriteLineHelp("  -mwe=<n>, --max-warmup-error=<n>");
            ConsoleHelper.WriteLineHelp("      Max permissible error (in percent) as condition for finishing of WarmUp");
            ConsoleHelper.WriteLineHelp("  -pb=<false|true>, --print-benchmark=<false|true>");
            ConsoleHelper.WriteLineHelp("      Printing the report of each benchmark to the console");
            ConsoleHelper.WriteLineHelp("  -pa=<n>, --processor-affinity=<n>");
            ConsoleHelper.WriteLineHelp("      ProcessorAffinity");
            ConsoleHelper.WriteLineHelp("  -dw, --disable-warmup");
            ConsoleHelper.WriteLineHelp("      Disable WarmUp, equivalent of -mwc=0");
            ConsoleHelper.WriteLineHelp("  -s, --single");
            ConsoleHelper.WriteLineHelp("      Single result benchmark without WarmUp, equivalent of -mwc=0 -rc=1");
            ConsoleHelper.WriteLineHelp("  -of=<filename>, --output-file=<filename>");
            ConsoleHelper.WriteLineHelp("      Save results of benchmark competition to file");
            ConsoleHelper.NewLine();
            PrintAvailable();
        }

        private void PrintAvailable()
        {
            ConsoleHelper.WriteLineHelp("Available competitions:");
            int numberWidth = competitions.Length.ToString().Length;
            for (int i = 0; i < competitions.Length; i++)
                ConsoleHelper.WriteLineHelp(BenchmarkUtils.CultureFormat("  #{0} {1}", i.ToString().PadRight(numberWidth), competitions[i].Name));
            ConsoleHelper.NewLine();
            ConsoleHelper.NewLine();
        }

        private static bool Contains(string[] args, params string[] patterns)
        {
            args = args.Select(arg => arg.ToLower()).ToArray();
            patterns = patterns.Select(parrent => parrent.ToLower()).ToArray();
            return patterns.Any(args.Contains);
        }

        private static string[] GetArgValues(string[] args, params string[] patterns)
        {
            return (from arg in args
                    from pattern in patterns
                    where arg.StartsWith(pattern + "=", StringComparison.OrdinalIgnoreCase)
                    select arg.Substring(pattern.Length + 1)).ToArray();
        }

        private static int? GetInt32ArgValue(string[] args, params string[] patterns)
        {
            var values = GetArgValues(args, patterns);
            int result;
            if (values.Length > 0 && int.TryParse(values[0], out result))
                return result;
            return null;
        }

        private static bool? GetBoolArgValue(string[] args, params string[] patterns)
        {
            var values = GetArgValues(args, patterns);
            if (values.Length == 0)
                return null;
            return values[0].ToLower() == "true" || values[0] == "1";
        }

        private static string GetStringArgValue(string[] args, params string[] patterns)
        {
            var values = GetArgValues(args, patterns);
            if (values.Length == 0)
                return null;
            return values[0];
        }
    }
}
