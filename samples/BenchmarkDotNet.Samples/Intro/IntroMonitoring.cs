using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples.Intro
{
    [SimpleJob(RunStrategy.Monitoring, 1, 3, 5, id: "MonitoringJob")]
    [MemoryDiagnoser]
    public class IntroMonitoring
    {
        private int counter;
        private byte[] byteArray;

        [IterationSetup]
        public void IterationSetup()
        {
            counter = 5;
            byteArray = new byte[1024 * 1024]; // this allocation will be counted in MemoryDiagnoser
        }

        [Benchmark]
        public void Foo()
        {
            if (counter == 0)
                throw new InvalidOperationException("counter == 0");
            while (counter > 0)
            {
                counter--;
                Thread.Sleep(10);
            }
        }
    }
}