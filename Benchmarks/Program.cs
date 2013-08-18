using System;
using System.Linq;

namespace Benchmarks
{
    class Program
    {
        private static readonly ProgramRunner[] programs = new[]
            {
                new ProgramRunner("Increment", () => new IncrementProgram().Run()),
                new ProgramRunner("MultidimensionalArray", () => new MultidimensionalArrayProgram().Run()),
                new ProgramRunner("StaticField", () => new StaticFieldProgram().Run()),
                new ProgramRunner("ShiftVsMultiply", () => new ShiftVsMultiplyProgram().Run()), 
                new ProgramRunner("ReverseSort", () => new ReverseSortProgram().Run()), 
            };

        static void Main(string[] args)
        {
            var name = args.Length == 0 ? "" : args[0];
            while (true)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    PrintAvailable();
                    Console.WriteLine("Please, print name of a target program:");
                    name = Console.ReadLine();
                }
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var target = programs.FirstOrDefault(runner => runner.Name.ToLower().StartsWith(name.ToLower()));
                if (target == null)
                {
                    name = null;
                    continue;
                }
                Console.WriteLine("Target program: " + target.Name);
                target.Run();
                break;
            }
        }

        private static void PrintAvailable()
        {
            Console.WriteLine("Available programs:");
            Console.Write("  ");
            foreach (var runner in programs)
                Console.Write(runner.Name + " ");
            Console.WriteLine();
            Console.WriteLine();
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
