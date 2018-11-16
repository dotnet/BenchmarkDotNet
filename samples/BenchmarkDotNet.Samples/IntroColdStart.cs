using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.ColdStart, targetCount: 5)]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class IntroColdStart
    {
        private bool firstCall = true;

        [Benchmark]
        public void Foo()
        {
            if (firstCall == true)
            {
                firstCall = false;
                Console.WriteLine("// First call");
                Thread.Sleep(1000);
            }
            else
                Thread.Sleep(10);
        }
    }
}
