using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Settings;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private readonly Type[] competitions =
        {
            typeof(ArrayBoundEliminationCompetition),
            typeof(BitCountCompetition),
            typeof(ArrayIterationCompetition),
            typeof(ForeachArrayCompetition),
            typeof(ForeachListCompetition),
            typeof(IncrementCompetition),
            typeof(InstructionLevelParallelismCompetition),
            typeof(MatrixMultiplicationCompetition),
            typeof(MultidimensionalArrayCompetition),
            typeof(ReverseSortCompetition),
            typeof(ShiftVsMultiplyCompetition),
            typeof(SelectVsConvertAllCompetition),
            typeof(StackFrameCompetition),
            typeof(CacheConsciousBinarySearchCompetition)
        };

        private readonly BenchmarkConsoleLogger logger = new BenchmarkConsoleLogger();

        private void Run(string[] args)
        {
            Array.Sort(competitions, (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
            args = ReadArgumentList(args);
            RunCompetitions(args, CreateBenchmarkSettings(args));
        }

        private string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintHelp();
                logger.WriteLineHelp("Argument list is empty. Please, print the argument list:");
                args = ReadArgsLine();
                logger.NewLine();
            }
            return args;
        }

        private static Dictionary<string, object> CreateBenchmarkSettings(string[] args)
        {
            var settings = new Dictionary<string, object>();

            if (Contains(args, "-d", "--details"))
                BenchmarkSettings.DetailedMode.Set(settings, true);

            if (Contains(args, "-dw", "--disable-warmup"))
                BenchmarkSettings.MaxWarmUpIterationCount.Set(settings, (uint)0);

            if (Contains(args, "-s", "--single"))
            {
                BenchmarkSettings.MaxWarmUpIterationCount.Set(settings, (uint)0);
                BenchmarkSettings.TargetIterationCount.Set(settings, (uint)0);
            }

            uint? resultIterationCount = GetUInt32ArgValue(args, "-tc", "--target-count");
            if (resultIterationCount != null)
                BenchmarkSettings.TargetIterationCount.Set(settings, resultIterationCount.Value);

            uint? warmUpIterationCount = GetUInt32ArgValue(args, "-wc", "--warmup-count");
            if (warmUpIterationCount != null)
                BenchmarkSettings.WarmUpIterationCount.Set(settings, warmUpIterationCount.Value);

            uint? maxWarmUpIterationCount = GetUInt32ArgValue(args, "-mwc", "--max-warmup-count");
            if (maxWarmUpIterationCount != null)
                BenchmarkSettings.MaxWarmUpIterationCount.Set(settings, maxWarmUpIterationCount.Value);

            uint? maxWarpUpError = GetUInt32ArgValue(args, "-mwe", "--max-warmup-error");
            if (maxWarpUpError != null)
                BenchmarkSettings.MaxWarmUpError.Set(settings, maxWarpUpError.Value / 100.0);

            uint? processorAffinity = GetUInt32ArgValue(args, "-pa", "--processor-affinity");
            if (processorAffinity != null)
                BenchmarkSettings.ProcessorAffinity.Set(settings, processorAffinity.Value);

            return settings;
        }

        private void RunCompetitions(string[] args, Dictionary<string, object> settings)
        {
            bool runAll = Contains(args, "-a", "--all");

            for (int i = 0; i < competitions.Length; i++)
            {
                var competition = competitions[i];
                if (runAll || args.Any(arg => competition.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i))
                {
                    logger.WriteLineHeader("Target program: " + competition.Name);
                    new BenchmarkRunner(settings).RunCompetition(Activator.CreateInstance(competition));
                    logger.NewLine();
                }
            }
        }

        private void PrintHelp()
        {
            logger.WriteLineHelp("Usage: Benchmarks <competitions-names> [<arguments>]");
            logger.WriteLineHelp("Arguments:");
            logger.WriteLineHelp("  -a, --all");
            logger.WriteLineHelp("      Run all available competitions");
            logger.WriteLineHelp("  -d, --details");
            logger.WriteLineHelp("      Show detailed results");
            logger.WriteLineHelp("  -tc=<n>, --target-count=<n>");
            logger.WriteLineHelp("      Target set iteration count");
            logger.WriteLineHelp("  -wc=<n>, --warmup-count=<n>");
            logger.WriteLineHelp("      WarmUp set default iteration count");
            logger.WriteLineHelp("  -mwc=<n>, --max-warmup-count=<n>");
            logger.WriteLineHelp("      WarmUp set max iteration count");
            logger.WriteLineHelp("  -mwe=<n>, --max-warmup-error=<n>");
            logger.WriteLineHelp("      Max permissible error (in percent) as condition for finishing of WarmUp");
            logger.WriteLineHelp("  -pb=<false|true>, --print-benchmark=<false|true>");
            logger.WriteLineHelp("      Printing the report of each benchmark to the console");
            logger.WriteLineHelp("  -pa=<n>, --processor-affinity=<n>");
            logger.WriteLineHelp("      ProcessorAffinity");
            logger.WriteLineHelp("  -dw, --disable-warmup");
            logger.WriteLineHelp("      Disable WarmUp, equivalent of -mwc=0");
            logger.WriteLineHelp("  -s, --single");
            logger.WriteLineHelp("      Single result benchmark without WarmUp, equivalent of -mwc=0 -rc=1");
            logger.NewLine();
            PrintAvailable();
        }

        private void PrintAvailable()
        {
            logger.WriteLineHelp("Available competitions:");
            int numberWidth = competitions.Length.ToString().Length;
            for (int i = 0; i < competitions.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), competitions[i].Name));
            logger.NewLine();
            logger.NewLine();
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

        private static uint? GetUInt32ArgValue(string[] args, params string[] patterns)
        {
            var values = GetArgValues(args, patterns);
            uint result;
            if (values.Length > 0 && uint.TryParse(values[0], out result))
                return result;
            return null;
        }

        public static string[] ReadArgsLine()
        {
            return (Console.ReadLine() ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
