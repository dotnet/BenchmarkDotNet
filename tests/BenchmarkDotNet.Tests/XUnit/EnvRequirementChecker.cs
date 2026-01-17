using BenchmarkDotNet.Environments;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BdnRuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Tests.XUnit;

public static class EnvRequirementChecker
{
    private const string MonoAotCompilerPathEnvVar = "MONOAOTLLVM_COMPILER_PATH";

    public static string? GetSkip(params EnvRequirement[] requirements) => requirements.Select(GetSkip).FirstOrDefault(skip => skip != null);

    internal static string? GetSkip(EnvRequirement requirement) => requirement switch
    {
        EnvRequirement.WindowsOnly => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Windows-only test",
        EnvRequirement.NonWindows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Non-Windows test",
        EnvRequirement.NonWindowsArm => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !IsArm() ? null : "Non-Windows+Arm test",
        EnvRequirement.NonLinux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? null : "Non-Linux test",
        EnvRequirement.FullFrameworkOnly => BdnRuntimeInformation.IsFullFramework ? null : "Full .NET Framework-only test",
        EnvRequirement.NonFullFramework => !BdnRuntimeInformation.IsFullFramework ? null : "Non-Full .NET Framework test",
        EnvRequirement.DotNetCoreOnly => BdnRuntimeInformation.IsNetCore ? null : ".NET/.NET Core-only test",
        EnvRequirement.NeedsPrivilegedProcess => IsPrivilegedProcess() ? null : "Needs authorization to perform security-relevant functions",
        EnvRequirement.MonoAotLlvmToolchain => IsMonoAotLlvmAvailable() ? null : $"Mono AOT LLVM toolchain not found. Set {MonoAotCompilerPathEnvVar} to the mono-sgen binary path",
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

    private static bool IsArm()
        => BdnRuntimeInformation.GetCurrentPlatform() is Platform.Arm64 or Platform.Arm or Platform.Armv6;

    private static bool IsMonoAotLlvmAvailable()
    {
        var compilerPath = Environment.GetEnvironmentVariable(MonoAotCompilerPathEnvVar);
        return !string.IsNullOrEmpty(compilerPath) && File.Exists(compilerPath);
    }
}
