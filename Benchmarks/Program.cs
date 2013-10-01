using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using BenchmarkDotNet;

namespace Benchmarks
{
    class Program
    {
        private static readonly ProgramRunner[] programs = new[]
            {
                new ProgramRunner("Increment", m => new IncrementProgram().Run(m)),
                new ProgramRunner("MultidimensionalArray", m => new MultidimensionalArrayProgram().Run(m)),
                new ProgramRunner("ArrayIteration", m => new ArrayIterationProgram().Run(m)),
                new ProgramRunner("ShiftVsMultiply", m => new ShiftVsMultiplyProgram().Run(m)), 
                new ProgramRunner("ReverseSort", m => new ReverseSortProgram().Run(m)),
                new ProgramRunner("MakeRefVsBoxing", m => new MakeRefVsBoxingProgram().Run(m)), 
                new ProgramRunner("ForeachArray", m => new ForeachArrayProgram().Run(m)), 
                new ProgramRunner("ForeachList", m => new ForeachListProgram().Run(m)), 
                new ProgramRunner("StackFrame", m => new StackFrameProgram().Run(m)),
            };

        private static readonly Manager manager = new Manager();

        static void Main(string[] args)
        {
            SetCulture();
            Array.Sort(programs, (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
            args = ReadArgumentList(args);
            ApplyBenchmarkSettings(args);
            RunPrograms(args);
        }

        private static void SetCulture()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
        }

        private static string[] ReadArgumentList(string[] args)
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

        private static void ApplyBenchmarkSettings(string[] args)
        {
            BenchmarkSettings.Instance.DetailedMode = Contains(args, "-d", "--details");

            if (Contains(args, "-dw", "--disable-warmup"))
                BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount = 0;

            if (Contains(args, "-s", "--single"))
            {
                BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount = 0;
                BenchmarkSettings.Instance.DefaultResultIterationCount = 1;
            }

            string outputFileName = GetStringArgValue(args, "-of", "--output-file");
            if (outputFileName != null)
                manager.OutputFileName = outputFileName;

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

        private static void RunPrograms(string[] args)
        {
            bool runAll = Contains(args, "-a", "--all");

            for (int i = 0; i < programs.Length; i++)
            {
                var program = programs[i];
                if (runAll || args.Any(arg => program.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i))
                {
                    ConsoleHelper.WriteLineHeader("Target program: " + program.Name);
                    program.Run(manager);
                    ConsoleHelper.NewLine();
                }
            }
        }

        private static void PrintHelp()
        {
            ConsoleHelper.WriteLineHelp("Usage: Benchmarks <programs-names> [<arguments>]");
            ConsoleHelper.WriteLineHelp("Arguments:");
            ConsoleHelper.WriteLineHelp("  -a, --all");
            ConsoleHelper.WriteLineHelp("      Run all available programs");
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

        private static void PrintAvailable()
        {
            ConsoleHelper.WriteLineHelp("Available programs:");
            int numberWidth = programs.Length.ToString().Length;
            for (int i = 0; i < programs.Length; i++)
                ConsoleHelper.WriteLineHelp(BenchmarkUtils.CultureFormat("  #{0} {1}", i.ToString().PadRight(numberWidth), programs[i].Name));
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

        class ProgramRunner
        {
            public string Name { get; private set; }
            public Action<Manager> Run { get; private set; }

            public ProgramRunner(string name, Action<Manager> run)
            {
                Name = name;
                Run = run;
            }
        }
    }
}
