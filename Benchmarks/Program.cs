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
                new ProgramRunner("Increment", () => new IncrementProgram().Run()),
                new ProgramRunner("MultidimensionalArray", () => new MultidimensionalArrayProgram().Run()),
                new ProgramRunner("ArrayIteration", () => new ArrayIterationProgram().Run()),
                new ProgramRunner("ShiftVsMultiply", () => new ShiftVsMultiplyProgram().Run()), 
                new ProgramRunner("ReverseSort", () => new ReverseSortProgram().Run()),
                new ProgramRunner("MakeRefVsBoxing", () => new MakeRefVsBoxingProgram().Run()), 
                new ProgramRunner("ForeachArray", () => new ForeachArrayProgram().Run()), 
                new ProgramRunner("ForeachList", () => new ForeachListProgram().Run()), 
            };

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
            while (args.Length == 0)
            {
                PrintHelp();
                ConsoleHelper.WriteLineHelp("Argument list is empty. Please, print argument list:");
                args = ConsoleHelper.ReadArgsLine();
                ConsoleHelper.NewLine();
            }
            BenchmarkSettings.Instance.DetailedMode = Contains(args, "-d", "--details");
            bool runAll = Contains(args, "-a", "--all");
            foreach (var program in programs)
                if (runAll || args.Any(arg => program.Name.ToLower().StartsWith(arg.ToLower())))
                {
                    ConsoleHelper.WriteLineHeader("Target program: " + program.Name);
                    program.Run();
                    ConsoleHelper.NewLine();
                }
        }

        private static void PrintHelp()
        {
            ConsoleHelper.WriteLineHelp("Usage: Benchmarks <programs-names> [-a|--all] [-d|--details]");
            ConsoleHelper.WriteLineHelp("  -a, --all      Run all available programs");
            ConsoleHelper.WriteLineHelp("  -d, --details  Show detailed results");
            ConsoleHelper.NewLine();
            PrintAvailable();
        }

        private static void PrintAvailable()
        {
            ConsoleHelper.WriteLineHelp("Available programs:");
            foreach (var program in programs)
                ConsoleHelper.WriteLineHelp("  " + program.Name);
            ConsoleHelper.NewLine();
            ConsoleHelper.NewLine();
        }

        private static bool Contains(string[] args, params string[] patterns)
        {
            return patterns.Any(args.Contains);
        }

        class ProgramRunner
        {
            public string Name { get; private set; }
            public Action Run { get; private set; }

            public ProgramRunner(string name, Action run)
            {
                Name = name;
                Run = run;
            }
        }
    }
}
