using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Samples.Framework;
using System;
using System.Linq;

namespace BenchmarkDotNet.Samples.Uap
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Configs.ManualConfig config = new Configs.ManualConfig();
            var uapJob = new Job("IntegrationTestJob", new EnvMode(new UapRuntime(name: "IntegrationTestJob",
                devicePortalUri: "https://localhost:50443",
                username: "uap_integration",
                password: "uap_integration",
                uapBinariesPath: args[0],
                platform: Platform.X64)));
            config.Add(uapJob);
            config.KeepBenchmarkFiles = true;

            var summary = BenchmarkRunner.Run(typeof(Framework_DateTime), config);
            Console.Write("attach");
            Console.ReadLine();

            return summary.Reports.Count();
        }
    }
}
