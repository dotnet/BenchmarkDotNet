using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroCustomEnvironmentInfo
    {
        [CustomEnvironmentInfo]
        public static string IsServerGC() => $"IsServerGC={HostEnvironmentInfo.GetCurrent().IsServerGC}";

        [CustomEnvironmentInfo]
        public static IEnumerable<string> CommandLineArgs() =>
            Environment.GetCommandLineArgs().Select((arg, i) => $"args[{i}]={arg}");

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}
