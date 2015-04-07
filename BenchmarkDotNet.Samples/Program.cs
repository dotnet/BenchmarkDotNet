using System;
using Benchmarks;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main()
        {
            const string separator = "***************************************************************************";
            var samples = new ISample[]
            {
                //                new ExplicitlySingleBenchmark(),
                //                new ExplicitlyBenchmarkCompetition(),
                new AttributesSample(),
            };
            foreach (var sample in samples)
            {
                var sampleName = sample.GetType().Name;
                Console.WriteLine(separator);
                Console.WriteLine("***** " + sampleName + " " + separator.Substring(7 + sampleName.Length));
                Console.WriteLine(separator);
                sample.Run();
            }
        }
    }
}
