﻿#if !UAP
using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.ProjectJson;
using BenchmarkDotNet.Toolchains.Uap;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        private static readonly Lazy<bool> isUsingProjectJson = new Lazy<bool>(IsUsingProjectJson);

        internal static IToolchain GetToolchain(this Job job)
        {
            return job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvMode.RuntimeCharacteristic, EnvResolver.Instance));
        }

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case ClrRuntime clr:
                case MonoRuntime mono:
#if CLASSIC
                    return new Roslyn.RoslynToolchain();
#else
                    return isUsingProjectJson.Value ? ProjectJsonNet46Toolchain.Instance : CsProjNet46Toolchain.Instance;
#endif
                case CoreRuntime core:
                    return isUsingProjectJson.Value ? ProjectJsonCoreToolchain.Current.Value : CsProjCoreToolchain.Current.Value;
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
        }

        private static bool IsUsingProjectJson() => 
            HostEnvironmentInfo.GetCurrent().DotNetCliVersion.Value.Contains("preview") 
            && SolutionDirectoryContainsProjectJsonFiles();

        private static bool SolutionDirectoryContainsProjectJsonFiles()
        {
            if (!DotNetCliGenerator.GetSolutionRootDirectory(out var solutionRootDirectory))
            {
                solutionRootDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            return solutionRootDirectory.EnumerateFiles("project.json", SearchOption.AllDirectories).Any();
        }
    }
}
#endif