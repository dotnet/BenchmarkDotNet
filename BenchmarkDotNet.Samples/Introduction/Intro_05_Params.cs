using System;
using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    [BenchmarkTask(1, warmupIterationCount: 3, targetIterationCount: 5, intParams: new[] { 50, 100, 150, 200 })]
    public class Intro_05_Params
    {
        public Intro_05_Params()
        {
            Value = BenchmarkState.Instance.IntParam;
        }

        public int Value;

        private readonly Random random = new Random();

        [Benchmark]
        public void RunSlow()
        {
            Thread.Sleep(Value * 2);
        }

        [Benchmark]
        public void RunFast()
        {
            var offset = (random.Next(201) - 100) * 0.01;
            Thread.Sleep((int)(Value * (1 + offset)));
        }
    }
}