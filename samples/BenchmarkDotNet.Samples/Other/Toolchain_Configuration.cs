using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Samples.Other
{
    [Config(typeof(Config))]
    public class Toolchain_Configuration
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var toolchain = CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp11, "CustomConfiguration");

                Add(new Job
                {
                    Run = { LaunchCount = 3, TargetCount = 100 }
                }.With(toolchain));
            }
        }

        [Benchmark]
        public void Sleep()
        {
#if CUSTOMCONFIGURATION
            Thread.Sleep(10);
#else
            Thread.Sleep(1);
#endif
        }
    }
}