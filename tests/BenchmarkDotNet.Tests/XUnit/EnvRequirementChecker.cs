using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
        EnvRequirement.NeedsPrivilegedProcess => IsPrivilegedProcess() ? null : "Needs authorization to perform security-relevant functions",
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unknown value")
    };

    private static bool IsPrivilegedProcess()
    {
#if NET462
        using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(currentUser).IsInRole(WindowsBuiltInRole.Administrator);
#else
        return Environment.IsPrivilegedProcess;
#endif
    }

    private static bool IsRuntime(RuntimeMoniker moniker) => BdnRuntimeInformation.GetCurrentRuntime().RuntimeMoniker == moniker;
}