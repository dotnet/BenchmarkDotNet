using System;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroUnstable
    {
        private int counter = 0;

        [Benchmark]
        public void Foo()
        {
            Thread.Sleep(100 - (int) (Math.Log(++counter) * 10));
        }
    }
}