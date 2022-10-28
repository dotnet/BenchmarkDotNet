using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.Monitoring, iterationCount: 10, id: "MonitoringJob")]
    [MinColumn, Q1Column, Q3Column, MaxColumn]
    public class IntroMonitoring
    {
        private Random random = new Random(42);

        [Benchmark]
        public void Foo()
        {
            Thread.Sleep(random.Next(10) * 10);
        }
    }
}