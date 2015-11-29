using System;
using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    [BenchmarkTask(1, warmupIterationCount: 1, targetIterationCount: 3)]
    public class Intro_07_Warnings
    {
        private readonly Random random = new Random();

        [Benchmark]
        public void BigStdDev()
        {
            Thread.Sleep(random.Next(2) == 0 ? 100 : 1000);
        }
    }
}