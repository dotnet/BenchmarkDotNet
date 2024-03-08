using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    [BenchmarkDescription("Used to be 'IntroRenameTest', now is 'My Renamed Test'")]
    public class IntroRenameTest
    {
        // And define a method with the Benchmark attribute
        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        // You can write a description for your method.
        [Benchmark(Description = "Thread.Sleep(10)")]
        public void SleepWithDescription() => Thread.Sleep(10);
    }
}
