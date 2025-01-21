using System;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;
using BdnRuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Tests.XUnit;

public static class EnvRequirementChecker
{
    public static string? GetSkip(params EnvRequirement[] requirements) => requirements.Select(GetSkip).FirstOrDefault(skip => skip != null);

    internal static string? GetSkip(EnvRequirement requirement) => requirement switch
    {
        EnvRequirement.WindowsOnly => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Windows-only test",
        EnvRequirement.NonWindows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Non-Windows test",
        EnvRequirement.NonLinux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? null : "Non-Linux test",
        EnvRequirement.FullFrameworkOnly => BdnRuntimeInformation.IsFullFramework ? null : "Full .NET Framework-only test",
        EnvRequirement.NonFullFramework => !BdnRuntimeInformation.IsFullFramework ? null : "Non-Full .NET Framework test",
        EnvRequirement.DotNetCoreOnly => BdnRuntimeInformation.IsNetCore ? null : ".NET/.NET Core-only test",
        EnvRequirement.DotNetCore30Only => IsRuntime(RuntimeMoniker.NetCoreApp30) ? null : ".NET Core 3.0-only test",
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unknown value")
    };

    private static bool IsRuntime(RuntimeMoniker moniker) => BdnRuntimeInformation.GetCurrentRuntime().RuntimeMoniker == moniker;
}