using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using System;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class IntroCustomEnvironmentInfo
    {
        [CustomEnvironmentInfo]
        public static string IsServerGC() => $"IsServerGC={HostEnvironmentInfo.GetCurrent().IsServerGC}";

        [CustomEnvironmentInfo]
        public static string[] CommandLineArgs() => Environment.GetCommandLineArgs();

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}
