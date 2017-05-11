using System;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Uap.Running
{
    public static class BenchmarkRunner
    {
        public static Summary Run<T>(IConfig config = null) 
            => BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config), config, GetToolchain);

        public static Summary Run(Type type, IConfig config = null) 
            => BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(type, config), config, GetToolchain);

        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null) 
            => BenchmarkRunnerCore.Run(BenchmarkConverter.MethodsToBenchmarks(type, methods, config), config, GetToolchain);

        public static Summary Run(Benchmark[] benchmarks, IConfig config) 
            => BenchmarkRunnerCore.Run(benchmarks, config, GetToolchain);

        private static IToolchain GetToolchain(Job job)
        {
            return job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvMode.RuntimeCharacteristic, EnvResolver.Instance));
        }

        private static IToolchain GetToolchain(this Runtime runtime)
        {
#if UAP
            throw new Exception("To run UAP benchmarks your host process can't be UAP (.NET 4.6 or .NET Core 1.1");
#else
            switch (runtime)
            {
                case ClrRuntime clr:
                case MonoRuntime mono:
#if CLASSIC
                    return new Toolchains.Roslyn.RoslynToolchain();
#else
                    return new CsProjNet46Toolchain();
#endif
                case CoreRuntime core:
                    return CsProjCoreToolchain.Current.Value;
                case UapRuntime uap:
                    if (!string.IsNullOrEmpty(uap.CsfrCookie))
                    {
                        return new UapToolchain(new UapToolchainConfig() { CSRFCookieValue = uap.CsfrCookie, DevicePortalUri = uap.DevicePortalUri, UAPBinariesFolder = uap.UapBinariesPath, WMIDCookieValue = uap.WmidCookie, Platform = uap.Platform });
                    }
                    else
                    {
                        return new UapToolchain(new UapToolchainConfig() { Username = uap.Username, DevicePortalUri = uap.DevicePortalUri, Password = uap.Password, UAPBinariesFolder = uap.UapBinariesPath, Platform = uap.Platform });
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
#endif
        }

    }
}