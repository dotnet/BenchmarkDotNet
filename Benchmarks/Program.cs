using System;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    class Program
    {
        private static BenchmarkProgram[] programs = new[]
            {
                new BenchmarkProgram("Increment", () => new IncrementBenchmark().Run()),
                new BenchmarkProgram("MultidimensionalArray", () => new MultidimensionalArrayBenchmark().Run()),
                new BenchmarkProgram("StaticField", () => new StaticFieldBenchmark().Run())
            };

        static void Main(string[] args)
        {
            new Benchmark().Run(() => IncrementBenchmark.After());
            return;
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

        class BenchmarkProgram
        {
            public string Name { get; set; }
            public Action Run { get; set; }

            public BenchmarkProgram(string name, Action run)
            {
                Name = name;
                Run = run;
            }
        }
    }
}
