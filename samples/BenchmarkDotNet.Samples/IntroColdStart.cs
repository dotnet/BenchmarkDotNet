using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class IntroColdStart
    {
        private bool firstCall;

        [Benchmark]
        public void Foo()
        {
            if (firstCall == false)
            {
                firstCall = true;
                Console.WriteLine("// First call");
                Thread.Sleep(1000);
            }
            else
                Thread.Sleep(10);
        }
    }
}