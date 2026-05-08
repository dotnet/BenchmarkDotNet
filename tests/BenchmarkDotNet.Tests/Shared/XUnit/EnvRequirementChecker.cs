using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BdnRuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Tests.XUnit;

public static class EnvRequirementChecker
{
    public static string? GetSkip(params EnvRequirement[] requirements) => requirements.Select(GetSkip).FirstOrDefault(skip => skip != null);

    internal static string? GetSkip(EnvRequirement requirement) => requirement switch
    {
        EnvRequirement.WindowsOnly => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Windows-only test",
        EnvRequirement.NonWindows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Non-Windows test",
        EnvRequirement.NonWindowsArm => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !IsArm() ? null : "Non-Windows+Arm test",
        EnvRequirement.NonLinux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? null : "Non-Linux test",
        EnvRequirement.NonLinuxArm => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || !IsArm() ? null : "Non-Linux+Arm test",
        EnvRequirement.FullFrameworkOnly => BdnRuntimeInformation.IsFullFramework ? null : "Full .NET Framework-only test",
        EnvRequirement.NonFullFramework => !BdnRuntimeInformation.IsFullFramework ? null : "Non-Full .NET Framework test",
        EnvRequirement.DotNetCoreOnly => BdnRuntimeInformation.IsNetCore ? null : ".NET/.NET Core-only test",
        EnvRequirement.NeedsPrivilegedProcess => IsPrivilegedProcess() ? null : "Needs authorization to perform security-relevant functions",
        EnvRequirement.NonGitHubDraftPR => !IsGitHubDraftPR() ? null : "GitHub draft PR",
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unknown value")
    };

    private static bool IsPrivilegedProcess()
    {
#if NETFRAMEWORK
        using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(currentUser).IsInRole(WindowsBuiltInRole.Administrator);
#else
        return Environment.IsPrivilegedProcess;
#endif
    }

    private static bool IsArm()
        => BdnRuntimeInformation.GetCurrentPlatform() is Platform.Arm64 or Platform.Arm or Platform.Armv6;

    /// <summary>
    /// Check current test is running on GitHub Actions and PR is a draft PR.
    /// </summary>
    private static bool IsGitHubDraftPR()
        => Environment.GetEnvironmentVariable("GITHUB_ACTION").IsNotBlank()
        && Environment.GetEnvironmentVariable("IS_DRAFT_PR") == "true"; // `IS_DRAFT_PR` is set by CI workflow
}
