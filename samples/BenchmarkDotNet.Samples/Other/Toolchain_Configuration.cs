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
            private const string Configuration = "CustomConfiguration";

            public Config()
            {
                var toolchain = CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp11, Configuration);

                Add(new Job
                {
                    Run = { LaunchCount = 3, TargetCount = 10 }
                }.With(toolchain));
            }
        }

        [Benchmark]
        public void Sleep()
        {
#if CUSTOMCONFIGURATION
            Thread.Sleep(5);
#else
            Thread.Sleep(1);
#endif
        }
    }
}