using System;
using BenchmarkDotNet.Settings;

namespace BenchmarkDotNet.Samples
{
    public class ExplicitlySingleBenchmark : ISample
    {
        private const int IterationCount = 100000001;

        public void Run()
        {
            var benchmark = new Benchmark("i++", () => After());
            Console.WriteLine("Default: ");
            new BenchmarkRunner().Run(benchmark);
            Console.WriteLine();
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Detailed: ");
            new BenchmarkRunner(BenchmarkSettings.Build(BenchmarkSettings.DetailedMode.Create(true))).Run(benchmark);
        }

        public int After()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }
    }
}