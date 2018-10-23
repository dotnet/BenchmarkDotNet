using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroCustomEnvironmentInfo
    {
        //[CustomEnvironmentInfo]
        //public static string IsServerGC() => $"IsServerGC={HostEnvironmentInfo.GetCurrent().IsServerGC}";

        [CustomEnvironmentInfo]
        public static IEnumerable<string> CommandLineArgs() =>
            Environment.GetCommandLineArgs().Select((arg, i) => $"args[{i}]={arg}");

        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        [Benchmark(Description = "Thread.Sleep(10)")]
        public void SleepWithDescription() => Thread.Sleep(10);
    }
}
