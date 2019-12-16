using System;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [FirstCallColumn]
    public class IntroFirstCallColumn
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