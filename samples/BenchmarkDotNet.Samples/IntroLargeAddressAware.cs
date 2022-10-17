using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class IntroLargeAddressAware
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.Default
                    .WithRuntime(ClrRuntime.Net462)
                    .WithPlatform(Platform.X86)
                    .WithLargeAddressAware(value: RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    .WithId("Framework"));
            }
        }

        [Benchmark]
        public void AllocateMoreThan2GB()
        {
            const int oneGB = 1024 * 1024 * 1024;
            const int halfGB = oneGB / 2;
            byte[] bytes1 = new byte[oneGB];
            byte[] bytes2 = new byte[oneGB];
            byte[] bytes3 = new byte[halfGB];
            GC.KeepAlive(bytes1);
            GC.KeepAlive(bytes2);
            GC.KeepAlive(bytes3);
        }
    }
}
